namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Lists overdue submitted orders.
/// </summary>
public sealed record ListOverdueOrdersQuery : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = Array.Empty<string>();
}

/// <summary>
/// Handles <see cref="ListOverdueOrdersQuery"/>.
/// </summary>
public sealed class ListOverdueOrdersQueryHandler(
    IOrderRepository orderRepository,
    IActorProvider actorProvider,
    TimeProvider timeProvider) : IQueryHandler<ListOverdueOrdersQuery, Result<IReadOnlyList<Order>>>
{
    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOverdueOrdersQuery query, CancellationToken cancellationToken)
    {
        var actor = (await actorProvider.GetCurrentActorAsync(cancellationToken))
            .GetValueOrThrow("Actor must be present; authorization behavior guarantees this.");

        if (!actor.HasPermission(Permissions.OrdersReadAll) && !actor.HasPermission(Permissions.OrdersRead))
        {
            return Result.Fail<IReadOnlyList<Order>>(new Error.Forbidden("orders.overdue.read.forbidden", ResourceRef.For<Order>("overdue"))
            {
                Detail = "The current actor cannot read overdue orders.",
            });
        }

        var specification = new OverdueOrderSpecification(timeProvider.GetUtcNow().UtcDateTime);
        var orders = await orderRepository.QueryAsync(specification, cancellationToken);
        var filtered = actor.HasPermission(Permissions.OrdersReadAll)
            ? orders
            : orders.Where(order => actor.IsOwner(order.CreatedByActorId)).ToList();

        return Result.Ok<IReadOnlyList<Order>>(filtered.OrderBy(order => order.SubmittedAt.GetValueOrDefault(DateTime.MaxValue)).ToList());
    }
}
