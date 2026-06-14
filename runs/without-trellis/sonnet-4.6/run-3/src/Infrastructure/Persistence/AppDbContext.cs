using Domain.Customers;
using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<LineItem> LineItems => Set<LineItem>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeLineItemStates();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await NormalizeLineItemStatesAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.CustomerId);
            e.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            e.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            e.Property(c => c.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(c => c.Email).IsUnique();
            e.Property(c => c.PhoneNumber).HasMaxLength(30);
            e.OwnsOne(c => c.ShippingAddress, sa =>
            {
                sa.Property(a => a.Street).HasMaxLength(200).IsRequired();
                sa.Property(a => a.City).HasMaxLength(100).IsRequired();
                sa.Property(a => a.State).HasMaxLength(100).IsRequired();
                sa.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
                sa.Property(a => a.Country).HasMaxLength(100).IsRequired();
            });
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.ProductId);
            e.Property(p => p.ProductName).HasMaxLength(200).IsRequired();
            e.Property(p => p.SKU).HasMaxLength(20).IsRequired();
            e.HasIndex(p => p.SKU).IsUnique();
            e.Property(p => p.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.OrderId);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(o => o.CreatedByActorId).HasMaxLength(100).IsRequired();
            e.HasIndex(o => o.CustomerId);
            e.HasIndex(o => new { o.Status, o.SubmittedAt });
            e.HasMany(o => o.LineItems).WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
            e.Navigation(o => o.LineItems).HasField("_lineItems").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<LineItem>(e =>
        {
            e.HasKey(li => li.LineItemId);
            e.Property(li => li.ProductName).HasMaxLength(200).IsRequired();
            e.Property(li => li.UnitPrice).HasPrecision(18, 2);
        });
    }

    private void NormalizeLineItemStates()
    {
        foreach (var entry in ChangeTracker.Entries<LineItem>().Where(e => e.State == EntityState.Modified))
        {
            if (entry.GetDatabaseValues() == null)
            {
                entry.State = EntityState.Added;
            }
        }
    }

    private async Task NormalizeLineItemStatesAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in ChangeTracker.Entries<LineItem>().Where(e => e.State == EntityState.Modified))
        {
            if (await entry.GetDatabaseValuesAsync(cancellationToken) == null)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
