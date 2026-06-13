namespace OrderManagement.AntiCorruptionLayer;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core repository for the <see cref="Order"/> aggregate.</summary>
public sealed class OrderRepository(AppDbContext context)
    : RepositoryBase<Order, OrderId>(context), IOrderRepository;
