namespace OrderManagement.AntiCorruptionLayer.Repositories;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core implementation of <see cref="IOrderRepository"/>.</summary>
public sealed class OrderRepository(AppDbContext context)
    : RepositoryBase<Order, OrderId>(context), IOrderRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(
        CustomerId customerId,
        CancellationToken cancellationToken) =>
        await DbSet.AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Order>> ListOverdueAsync(DateTime asOf, CancellationToken cancellationToken) =>
        QueryAsync(new OverdueOrderSpecification(asOf), cancellationToken);
}
