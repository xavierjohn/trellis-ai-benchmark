namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// EF Core order repository.
/// </summary>
internal sealed class OrderRepository(AppDbContext context) : IOrderRepository
{
    public Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        context.Orders
            .Include("_lineItems")
            .FirstOrDefaultMaybeAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        await context.Orders
            .Include("_lineItems")
            .Where(order => order.CustomerId == customerId)
            .OrderBy(order => order.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> ListOverdueAsync(DateTime cutoff, CancellationToken cancellationToken) =>
        await context.Orders
            .Include("_lineItems")
            .Where(new OverdueOrderSpecification(cutoff))
            .OrderBy(order => order.Id)
            .ToListAsync(cancellationToken);

    public void Add(Order order) => context.Orders.Add(order);
}
