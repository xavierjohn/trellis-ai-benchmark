namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Product and quantity input for order creation.
/// </summary>
public sealed record DraftOrderLine(ProductId ProductId, LineItemQuantity Quantity);

/// <summary>
/// Creates a draft order.
/// </summary>
public sealed record CreateDraftOrderCommand : ICommand<Result<Order>>, IAuthorize
{
    public CustomerId CustomerId { get; }
    public IReadOnlyList<DraftOrderLine> Lines { get; }
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];

    private CreateDraftOrderCommand(CustomerId customerId, IReadOnlyList<DraftOrderLine> lines)
    {
        CustomerId = customerId;
        Lines = lines;
    }

    public static Result<CreateDraftOrderCommand> TryCreate(CustomerId customerId, IReadOnlyList<DraftOrderLine> lines) =>
        Result.Ensure(lines.Count > 0, Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."))
            .Combine(Result.Ensure(lines.Select(line => line.ProductId).Distinct().Count() == lines.Count,
                Error.InvalidInput.ForField("lineItems", "duplicate_product", "The same product cannot appear in multiple line items.")))
            .Map(_ => new CreateDraftOrderCommand(customerId, lines));
}

/// <summary>
/// Handles draft order creation.
/// </summary>
public sealed class CreateDraftOrderCommandHandler(
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    IOrderRepository orderRepository,
    IActorProvider actorProvider,
    TimeProvider timeProvider) : ICommandHandler<CreateDraftOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.FindByIdAsync(command.CustomerId, cancellationToken);
        if (customer.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Customer>(command.CustomerId)) { Detail = $"Customer {command.CustomerId} not found." });

        var productIds = command.Lines.Select(line => line.ProductId).ToArray();
        var products = await productRepository.FindByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Length)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>()) { Detail = "One or more products were not found." });

        var productById = products.ToDictionary(product => product.Id);
        var actor = (await actorProvider.GetCurrentActorAsync(cancellationToken)).GetValueOrThrow("Actor must be present after authorization.");
        var orderLines = command.Lines.Select(line => (productById[line.ProductId], line.Quantity)).ToArray();

        return Order.TryCreate(command.CustomerId, orderLines, actor.Id.Value, timeProvider)
            .Tap(orderRepository.Add);
    }
}

/// <summary>
/// Adds a line item to a draft order.
/// </summary>
public sealed record AddLineItemCommand(OrderId OrderId, ProductId ProductId, LineItemQuantity Quantity)
    : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>
/// Handles line-item additions.
/// </summary>
public sealed class AddLineItemCommandHandler(IOrderRepository orderRepository, IProductRepository productRepository)
    : ICommandHandler<AddLineItemCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(AddLineItemCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (order.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." });

        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(command.ProductId)) { Detail = $"Product {command.ProductId} not found." });

        var orderValue = order.GetValueOrThrow("Order presence checked above.");
        var productValue = product.GetValueOrThrow("Product presence checked above.");
        return orderValue.AddLineItem(productValue, command.Quantity);
    }
}

/// <summary>
/// Removes a line item from a draft order.
/// </summary>
public sealed record RemoveLineItemCommand(OrderId OrderId, LineItemId LineItemId)
    : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>
/// Handles line-item removal.
/// </summary>
public sealed class RemoveLineItemCommandHandler(IOrderRepository orderRepository)
    : ICommandHandler<RemoveLineItemCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(RemoveLineItemCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        return order.HasNoValue
            ? Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." })
            : order.GetValueOrThrow("Order presence checked above.").RemoveLineItem(command.LineItemId);
    }
}

/// <summary>
/// Submits a draft order.
/// </summary>
public sealed record SubmitOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersSubmit];
}

/// <summary>
/// Approves a submitted order.
/// </summary>
public sealed record ApproveOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersApprove];
}

/// <summary>
/// Ships an approved order.
/// </summary>
public sealed record ShipOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersShip];
}

/// <summary>
/// Delivers a shipped order.
/// </summary>
public sealed record DeliverOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersDeliver];
}

/// <summary>
/// Cancels an order.
/// </summary>
public sealed record CancelOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCancel];
}

/// <summary>
/// Handles order submission.
/// </summary>
public sealed class SubmitOrderCommandHandler(IOrderRepository orderRepository, IProductRepository productRepository, TimeProvider timeProvider)
    : ICommandHandler<SubmitOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (order.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." });

        var orderValue = order.GetValueOrThrow("Order presence checked above.");
        var products = await productRepository.FindByIdsAsync(orderValue.LineItems.Select(line => line.ProductId).ToArray(), cancellationToken);
        return orderValue.Submit(products.ToDictionary(product => product.Id), timeProvider);
    }
}

/// <summary>
/// Handles order approval.
/// </summary>
public sealed class ApproveOrderCommandHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    : ICommandHandler<ApproveOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(ApproveOrderCommand command, CancellationToken cancellationToken) =>
        await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." })
            .BindAsync(order => order.Approve(timeProvider));
}

/// <summary>
/// Handles order shipping.
/// </summary>
public sealed class ShipOrderCommandHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    : ICommandHandler<ShipOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(ShipOrderCommand command, CancellationToken cancellationToken) =>
        await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." })
            .BindAsync(order => order.Ship(timeProvider));
}

/// <summary>
/// Handles order delivery.
/// </summary>
public sealed class DeliverOrderCommandHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    : ICommandHandler<DeliverOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(DeliverOrderCommand command, CancellationToken cancellationToken) =>
        await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." })
            .BindAsync(order => order.Deliver(timeProvider));
}

/// <summary>
/// Handles order cancellation with ownership authorization.
/// </summary>
public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IActorProvider actorProvider,
    TimeProvider timeProvider) : ICommandHandler<CancelOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (order.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." });

        var actor = (await actorProvider.GetCurrentActorAsync(cancellationToken)).GetValueOrThrow("Actor must be present after authorization.");
        var orderValue = order.GetValueOrThrow("Order presence checked above.");
        if (!actor.IsOwner(orderValue.CreatedByActorId) && !actor.HasPermission(Permissions.OrdersReadAll))
        {
            return Result.Fail<Order>(new Error.Forbidden("orders.cancel.owner", ResourceRef.For<Order>(command.OrderId))
            {
                Detail = "Only the order creator or an administrator can cancel this order.",
            });
        }

        var products = await productRepository.FindByIdsAsync(orderValue.LineItems.Select(line => line.ProductId).ToArray(), cancellationToken);
        return orderValue.Cancel(products.ToDictionary(product => product.Id), timeProvider);
    }
}
