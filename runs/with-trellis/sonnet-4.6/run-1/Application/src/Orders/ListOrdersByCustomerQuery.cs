namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Lists orders for a customer.
/// </summary>
public sealed record ListOrdersByCustomerQuery(CustomerId CustomerId) : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = Array.Empty<string>();
}

/// <summary>
/// Handles <see cref="ListOrdersByCustomerQuery"/>.
/// </summary>
public sealed class ListOrdersByCustomerQueryHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IActorProvider actorProvider) : IQueryHandler<ListOrdersByCustomerQuery, Result<IReadOnlyList<Order>>>
{
    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        var actor = (await actorProvider.GetCurrentActorAsync(cancellationToken))
            .GetValueOrThrow("Actor must be present; authorization behavior guarantees this.");

        var maybeCustomer = await customerRepository.FindByIdAsync(query.CustomerId, cancellationToken);
        if (!maybeCustomer.HasValue)
        {
            return Result.Fail<IReadOnlyList<Order>>(new Error.NotFound(ResourceRef.For<Customer>(query.CustomerId))
            {
                Detail = $"Customer {query.CustomerId} not found.",
            });
        }

        if (!actor.HasPermission(Permissions.OrdersReadAll) && !actor.HasPermission(Permissions.OrdersRead))
        {
            return Result.Fail<IReadOnlyList<Order>>(new Error.Forbidden("customer.orders.read.forbidden", ResourceRef.For<Customer>(query.CustomerId))
            {
                Detail = "The current actor cannot read orders.",
            });
        }

        var orders = await orderRepository.GetByCustomerIdAsync(query.CustomerId, cancellationToken);
        var filtered = actor.HasPermission(Permissions.OrdersReadAll)
            ? orders
            : orders.Where(order => actor.IsOwner(order.CreatedByActorId)).ToList();

        return Result.Ok<IReadOnlyList<Order>>(filtered.OrderByDescending(order => order.CreatedAt).ToList());
    }
}
