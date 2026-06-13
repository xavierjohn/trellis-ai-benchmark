using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Application.Orders;

public sealed class OrderService(
    IOrderRepository orders,
    ICustomerRepository customers,
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private DateTimeOffset Now => timeProvider.GetUtcNow();

    public async Task<Result<OrderResponse>> CreateDraftAsync(Actor actor, CreateOrderRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersCreate))
            return Forbidden(actor, Permissions.OrdersCreate);

        if (request.Lines is null || request.Lines.Count == 0)
            return Error.Validation("An order must have at least one line item.");

        var duplicateProducts = request.Lines
            .GroupBy(l => l.ProductId)
            .Any(g => g.Count() > 1);
        if (duplicateProducts)
            return Error.Validation("The same product cannot appear in multiple line items. Combine quantities instead.");

        foreach (var line in request.Lines)
        {
            if (line.Quantity < Order.MinQuantity || line.Quantity > Order.MaxQuantity)
                return Error.Validation($"Quantity must be between {Order.MinQuantity} and {Order.MaxQuantity}.");
        }

        if (!await customers.ExistsAsync(request.CustomerId, ct))
            return Error.NotFound($"Customer '{request.CustomerId}' was not found.");

        var productIds = request.Lines.Select(l => l.ProductId).ToList();
        var foundProducts = (await products.GetByIdsAsync(productIds, ct)).ToDictionary(p => p.Id);

        var lines = new List<(Order.LineRequest, Product)>();
        foreach (var line in request.Lines)
        {
            if (!foundProducts.TryGetValue(line.ProductId, out var product))
                return Error.NotFound($"Product '{line.ProductId}' was not found.");

            lines.Add((new Order.LineRequest(line.ProductId, line.Quantity), product));
        }

        var orderResult = Order.CreateDraft(request.CustomerId, actor.Id, lines, Now);
        if (orderResult.IsFailure)
            return orderResult.Error!;

        var order = orderResult.Value;
        await orders.AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return order.ToResponse();
    }

    public async Task<Result<OrderResponse>> AddLineItemAsync(Actor actor, Guid orderId, AddLineItemRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersCreate))
            return Forbidden(actor, Permissions.OrdersCreate);

        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var product = await products.GetByIdAsync(request.ProductId, ct);
        if (product is null)
            return Error.NotFound($"Product '{request.ProductId}' was not found.");

        var result = order.AddLineItem(product, request.Quantity);
        if (result.IsFailure)
            return result.Error!;

        await unitOfWork.SaveChangesAsync(ct);
        return order.ToResponse();
    }

    public async Task<Result<OrderResponse>> RemoveLineItemAsync(Actor actor, Guid orderId, Guid lineItemId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersCreate))
            return Forbidden(actor, Permissions.OrdersCreate);

        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var result = order.RemoveLineItem(lineItemId);
        if (result.IsFailure)
            return result.Error!;

        await unitOfWork.SaveChangesAsync(ct);
        return order.ToResponse();
    }

    public async Task<Result<OrderResponse>> SubmitAsync(Actor actor, Guid orderId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersSubmit))
            return Forbidden(actor, Permissions.OrdersSubmit);

        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var productMap = await LoadOrderProductsAsync(order, ct);
        var result = order.Submit(productMap, Now);
        if (result.IsFailure)
            return result.Error!;

        await unitOfWork.SaveChangesAsync(ct);
        return order.ToResponse();
    }

    public async Task<Result<OrderResponse>> ApproveAsync(Actor actor, Guid orderId, CancellationToken ct = default)
        => await TransitionAsync(actor, Permissions.OrdersApprove, orderId, (o, now) => o.Approve(now), ct);

    public async Task<Result<OrderResponse>> ShipAsync(Actor actor, Guid orderId, CancellationToken ct = default)
        => await TransitionAsync(actor, Permissions.OrdersShip, orderId, (o, now) => o.Ship(now), ct);

    public async Task<Result<OrderResponse>> DeliverAsync(Actor actor, Guid orderId, CancellationToken ct = default)
        => await TransitionAsync(actor, Permissions.OrdersDeliver, orderId, (o, now) => o.Deliver(now), ct);

    public async Task<Result<OrderResponse>> CancelAsync(Actor actor, Guid orderId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersCancel))
            return Forbidden(actor, Permissions.OrdersCancel);

        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        // Ownership check: must be the creator, or hold orders:read-all (admin).
        var isOwner = string.Equals(order.CreatedByActorId, actor.Id, StringComparison.Ordinal);
        if (!isOwner && !actor.Has(Permissions.OrdersReadAll))
            return Error.Forbidden($"Actor '{actor.Id}' may only cancel orders they created.");

        var productMap = await LoadOrderProductsAsync(order, ct);
        var result = order.Cancel(productMap, Now);
        if (result.IsFailure)
            return result.Error!;

        await unitOfWork.SaveChangesAsync(ct);
        return order.ToResponse();
    }

    public async Task<Result<OrderResponse>> GetAsync(Actor actor, Guid orderId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersRead))
            return Forbidden(actor, Permissions.OrdersRead);

        var order = await orders.GetByIdAsync(orderId, ct);
        return order is null
            ? Error.NotFound($"Order '{orderId}' was not found.")
            : order.ToResponse();
    }

    public async Task<Result<IReadOnlyList<OrderResponse>>> ListByCustomerAsync(Actor actor, Guid customerId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersReadAll))
            return Forbidden<IReadOnlyList<OrderResponse>>(actor, Permissions.OrdersReadAll);

        if (!await customers.ExistsAsync(customerId, ct))
            return Error.NotFound($"Customer '{customerId}' was not found.");

        var list = await orders.GetByCustomerAsync(customerId, ct);
        return Result<IReadOnlyList<OrderResponse>>.Success(list.Select(o => o.ToResponse()).ToList());
    }

    public async Task<Result<IReadOnlyList<OrderResponse>>> ListOverdueAsync(Actor actor, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersReadAll))
            return Forbidden<IReadOnlyList<OrderResponse>>(actor, Permissions.OrdersReadAll);

        var now = Now;
        var cutoff = now - OverdueOrderSpecification.Threshold;
        var candidates = await orders.GetSubmittedBeforeAsync(cutoff, ct);
        var overdue = candidates
            .Where(o => OverdueOrderSpecification.IsOverdue(o, now))
            .Select(o => o.ToResponse())
            .ToList();

        return Result<IReadOnlyList<OrderResponse>>.Success(overdue);
    }

    private async Task<Result<OrderResponse>> TransitionAsync(
        Actor actor, string permission, Guid orderId, Func<Order, DateTimeOffset, Result> transition, CancellationToken ct)
    {
        if (!actor.Has(permission))
            return Forbidden(actor, permission);

        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var result = transition(order, Now);
        if (result.IsFailure)
            return result.Error!;

        await unitOfWork.SaveChangesAsync(ct);
        return order.ToResponse();
    }

    private async Task<IReadOnlyDictionary<Guid, Product>> LoadOrderProductsAsync(Order order, CancellationToken ct)
    {
        var ids = order.LineItems.Select(li => li.ProductId).Distinct().ToList();
        var loaded = await products.GetByIdsAsync(ids, ct);
        return loaded.ToDictionary(p => p.Id);
    }

    private static Error Forbidden(Actor actor, string permission)
        => Error.Forbidden($"Actor '{actor.Id}' lacks permission '{permission}'.");

    private static Result<T> Forbidden<T>(Actor actor, string permission)
        => Error.Forbidden($"Actor '{actor.Id}' lacks permission '{permission}'.");
}
