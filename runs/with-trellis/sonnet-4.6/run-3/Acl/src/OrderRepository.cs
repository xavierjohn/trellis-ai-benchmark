namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

internal class OrderRepository(AppDbContext context) : RepositoryBase<Order, OrderId>(context), IOrderRepository
{
    protected override IQueryable<Order> BuildFindByIdQuery() =>
        DbSet.Include(order => order.LineItems);

    protected override IQueryable<Order> BuildQueryBase() =>
        DbSet.Include(order => order.LineItems).AsNoTracking();

    public async Task<IReadOnlyList<Order>> FindByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        await BuildQueryBase()
            .Where(order => order.CustomerId == customerId)
            .ToListAsync(cancellationToken);
}
