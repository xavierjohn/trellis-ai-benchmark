namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core implementation of <see cref="IOrderRepository"/>.</summary>
internal sealed class OrderRepository : RepositoryBase<Order, OrderId>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(
        CustomerId customerId, CancellationToken cancellationToken) =>
        await DbSet.Where(o => o.CustomerId == customerId).ToListAsync(cancellationToken);
}
