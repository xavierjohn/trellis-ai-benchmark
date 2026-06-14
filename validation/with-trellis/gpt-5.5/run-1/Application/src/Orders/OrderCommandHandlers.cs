namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Handler for draft order creation.</summary>
public sealed class CreateDraftOrderCommandHandler(
    ICustomerRepository customers,
    IProductRepository products,
    IOrderRepository orders,
    IActorProvider actorProvider)
    : ICommandHandler<CreateDraftOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Items.Count == 0)
            return Result.Fail<Order>(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));

        if (command.Items.Select(i => i.ProductId).Distinct().Count() != command.Items.Count)
            return Result.Fail<Order>(Error.InvalidInput.ForField("lineItems", "duplicate_product", "The same product cannot appear more than once."));

        var customer = await customers.FindByIdAsync(command.CustomerId, cancellationToken);
        if (customer.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Customer>(command.CustomerId)) { Detail = $"Customer {command.CustomerId.Value} not found." });

        var loadedProducts = await products.FindByIdsAsync(command.Items.Select(i => i.ProductId).ToArray(), cancellationToken);
        var missingProduct = command.Items.Select(i => i.ProductId).FirstOrDefault(id => loadedProducts.All(p => p.Id != id));
        if (missingProduct is not null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(missingProduct)) { Detail = $"Product {missingProduct.Value} not found." });

        var actor = (await actorProvider.GetCurrentActorAsync(cancellationToken)).GetValueOrThrow("Actor must be present; IAuthorize pipeline guarantees this.");
        var lineItems = command.Items
            .Select(item => (Product: loadedProducts.Single(product => product.Id == item.ProductId), item.Quantity))
            .ToArray();

        return Order.TryCreate(command.CustomerId, actor.Id, lineItems).Tap(orders.Add);
    }
}

/// <summary>Handler for line-item addition.</summary>
public sealed class AddLineItemCommandHandler(IOrderRepository orders, IProductRepository products)
    : ICommandHandler<AddLineItemCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(AddLineItemCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        if (order.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });

        var product = await products.FindByIdAsync(command.ProductId, cancellationToken);
        if (product.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(command.ProductId)) { Detail = $"Product {command.ProductId.Value} not found." });

        order.TryGetValue(out var orderValue);
        product.TryGetValue(out var productValue);
        return orderValue!.AddLineItem(productValue!, command.Quantity);
    }
}

/// <summary>Handler for line-item removal.</summary>
public sealed class RemoveLineItemCommandHandler(IOrderRepository orders)
    : ICommandHandler<RemoveLineItemCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(RemoveLineItemCommand command, CancellationToken cancellationToken)
    {
        var orderResult = await orders.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });

        return orderResult.Bind(order => order.RemoveLineItem(command.LineItemId));
    }
}

/// <summary>Handler for order submission.</summary>
public sealed class SubmitOrderCommandHandler(IOrderRepository orders, IProductRepository products, TimeProvider timeProvider)
    : ICommandHandler<SubmitOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        if (maybeOrder.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });

        maybeOrder.TryGetValue(out var loadedOrder);
        var order = loadedOrder!;
        if (order.Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("state.machine.invalid.transition", $"Trigger 'Submit' is not permitted from state '{order.Status}'."));

        var loadedProducts = await products.FindByIdsAsync(order.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken);
        foreach (var line in order.LineItems)
        {
            var product = loadedProducts.SingleOrDefault(p => p.Id == line.ProductId);
            if (product is null)
                return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(line.ProductId)) { Detail = $"Product {line.ProductId.Value} not found." });

            var stockResult = product.EnsureHasStock(line.Quantity);
            if (stockResult.IsFailure)
                return Result.Fail<Order>(stockResult.Error!);
        }

        foreach (var line in order.LineItems)
        {
            var product = loadedProducts.Single(p => p.Id == line.ProductId);
            var reserveResult = product.ReserveStock(line.Quantity);
            if (reserveResult.IsFailure)
                return Result.Fail<Order>(reserveResult.Error!);
        }

        return order.Submit(timeProvider);
    }
}

/// <summary>State-transition handler base.</summary>
public abstract class OrderTransitionHandler<TCommand>(IOrderRepository orders, TimeProvider timeProvider)
    where TCommand : ICommand<Result<Order>>
{
    /// <summary>Loads an order and applies a transition.</summary>
    protected async ValueTask<Result<Order>> ApplyAsync(OrderId orderId, Func<Order, TimeProvider, Result<Order>> transition, CancellationToken cancellationToken)
    {
        var orderResult = await orders.FindByIdAsync(orderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(orderId)) { Detail = $"Order {orderId.Value} not found." });

        return orderResult.Bind(order => transition(order, timeProvider));
    }
}

/// <summary>Approve order handler.</summary>
public sealed class ApproveOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    : OrderTransitionHandler<ApproveOrderCommand>(orders, timeProvider), ICommandHandler<ApproveOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public ValueTask<Result<Order>> Handle(ApproveOrderCommand command, CancellationToken cancellationToken) =>
        ApplyAsync(command.OrderId, static (order, tp) => order.Approve(tp), cancellationToken);
}

/// <summary>Ship order handler.</summary>
public sealed class ShipOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    : OrderTransitionHandler<ShipOrderCommand>(orders, timeProvider), ICommandHandler<ShipOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public ValueTask<Result<Order>> Handle(ShipOrderCommand command, CancellationToken cancellationToken) =>
        ApplyAsync(command.OrderId, static (order, tp) => order.Ship(tp), cancellationToken);
}

/// <summary>Deliver order handler.</summary>
public sealed class DeliverOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    : OrderTransitionHandler<DeliverOrderCommand>(orders, timeProvider), ICommandHandler<DeliverOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public ValueTask<Result<Order>> Handle(DeliverOrderCommand command, CancellationToken cancellationToken) =>
        ApplyAsync(command.OrderId, static (order, tp) => order.Deliver(tp), cancellationToken);
}

/// <summary>Cancel order handler.</summary>
public sealed class CancelOrderCommandHandler(IOrderRepository orders, IProductRepository products, TimeProvider timeProvider)
    : ICommandHandler<CancelOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        if (maybeOrder.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });

        maybeOrder.TryGetValue(out var loadedOrder);
        var order = loadedOrder!;
        var releaseStock = order.ShouldReleaseStockOnCancel();
        var cancelResult = order.Cancel(timeProvider);
        if (cancelResult.IsFailure)
            return cancelResult;

        if (!releaseStock)
            return cancelResult;

        var loadedProducts = await products.FindByIdsAsync(order.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken);
        foreach (var line in order.LineItems)
        {
            var product = loadedProducts.SingleOrDefault(p => p.Id == line.ProductId);
            if (product is null)
                return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(line.ProductId)) { Detail = $"Product {line.ProductId.Value} not found." });

            var releaseResult = product.ReleaseStock(line.Quantity);
            if (releaseResult.IsFailure)
                return Result.Fail<Order>(releaseResult.Error!);
        }

        return cancelResult;
    }
}
