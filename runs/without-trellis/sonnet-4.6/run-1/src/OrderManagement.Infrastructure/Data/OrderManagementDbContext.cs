using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Data.Configurations;

namespace OrderManagement.Infrastructure.Data;

public class OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<LineItem> LineItems => Set<LineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new LineItemConfiguration());
    }
}
