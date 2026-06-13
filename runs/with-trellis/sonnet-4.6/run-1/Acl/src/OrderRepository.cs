namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Order repository implementation.
/// </summary>
internal sealed class OrderRepository(AppDbContext context) : RepositoryBase<Order, OrderId>(context), IOrderRepository
{
    /// <inheritdoc />
    protected override IQueryable<Order> BuildFindByIdQuery() =>
        DbSet.Include("_lineItems");

    /// <inheritdoc />
    protected override IQueryable<Order> BuildQueryBase() =>
        DbSet.AsNoTracking().Include("_lineItems");

    /// <inheritdoc />
    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        await DbSet.AsNoTracking()
            .Include("_lineItems")
            .Where(order => order.CustomerId == customerId)
            .ToListAsync(cancellationToken);
}
