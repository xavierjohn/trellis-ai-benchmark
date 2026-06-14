using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<OrderManagementDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=OrderManagement.db"));
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;
        var (status, title) = exception switch
        {
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainValidationException => (StatusCodes.Status422UnprocessableEntity, "Validation Error"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
            JsonException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception?.Message,
            Type = $"https://httpstatuses.com/{status}"
        });
    });
});

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        context.Request.Query["api-version"] != ApiVersion.Value)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = "The api-version query parameter is required.",
            Type = "https://httpstatuses.com/400"
        });
        return;
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>().Database.EnsureCreatedAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapPost("/api/customers", async (CreateCustomerRequest request, OrderService service, HttpContext context) =>
{
    var customer = await service.CreateCustomerAsync(request, ActorProvider.GetActor(context), context.RequestAborted);
    return Results.Created($"/api/customers/{customer.Id}", CustomerResponse.From(customer));
});

app.MapPost("/api/products", async (CreateProductRequest request, OrderService service, HttpContext context) =>
{
    var product = await service.CreateProductAsync(request, ActorProvider.GetActor(context), context.RequestAborted);
    return Results.Created($"/api/products/{product.Id}", ProductResponse.From(product));
});

app.MapPost("/api/products/{id:guid}/stock-additions", async (Guid id, AddStockRequest request, OrderService service, HttpContext context) =>
    Results.Ok(ProductResponse.From(await service.AddStockAsync(id, request, ActorProvider.GetActor(context), context.RequestAborted))));

app.MapPost("/api/orders", async (CreateOrderRequest request, OrderService service, HttpContext context) =>
{
    var order = await service.CreateOrderAsync(request, ActorProvider.GetActor(context), context.RequestAborted);
    return Results.Created($"/api/orders/{order.Id}", OrderResponse.From(order));
});

app.MapPost("/api/orders/{id:guid}/line-items", async (Guid id, AddLineItemRequest request, OrderService service, HttpContext context) =>
    Results.Ok(OrderResponse.From(await service.AddLineItemAsync(id, request, ActorProvider.GetActor(context), context.RequestAborted))));

app.MapDelete("/api/orders/{id:guid}/line-items/{lineItemId:guid}", async (Guid id, Guid lineItemId, OrderService service, HttpContext context) =>
    Results.Ok(OrderResponse.From(await service.RemoveLineItemAsync(id, lineItemId, ActorProvider.GetActor(context), context.RequestAborted))));

app.MapPost("/api/orders/{id:guid}/submission", async (Guid id, OrderService service, HttpContext context) =>
    Results.Ok(OrderResponse.From(await service.SubmitOrderAsync(id, ActorProvider.GetActor(context), context.RequestAborted))));

app.MapPost("/api/orders/{id:guid}/approval", async (Guid id, OrderService service, HttpContext context) =>
    Results.Ok(OrderResponse.From(await service.ApproveOrderAsync(id, ActorProvider.GetActor(context), context.RequestAborted))));

app.MapPost("/api/orders/{id:guid}/shipment", async (Guid id, OrderService service, HttpContext context) =>
    Results.Ok(OrderResponse.From(await service.ShipOrderAsync(id, ActorProvider.GetActor(context), context.RequestAborted))));

app.MapPost("/api/orders/{id:guid}/delivery", async (Guid id, OrderService service, HttpContext context) =>
    Results.Ok(OrderResponse.From(await service.DeliverOrderAsync(id, ActorProvider.GetActor(context), context.RequestAborted))));

app.MapPost("/api/orders/{id:guid}/cancellation", async (Guid id, OrderService service, HttpContext context) =>
    Results.Ok(OrderResponse.From(await service.CancelOrderAsync(id, ActorProvider.GetActor(context), context.RequestAborted))));

app.MapGet("/api/orders/overdue", async (OrderService service, HttpContext context) =>
    Results.Ok((await service.ListOverdueOrdersAsync(ActorProvider.GetActor(context), context.RequestAborted)).Select(OrderResponse.From)));

app.MapGet("/api/orders/{id:guid}", async (Guid id, OrderService service, HttpContext context) =>
    Results.Ok(OrderResponse.From(await service.GetOrderAsync(id, ActorProvider.GetActor(context), context.RequestAborted))));

app.MapGet("/api/customers/{id:guid}/orders", async (Guid id, OrderService service, HttpContext context) =>
    Results.Ok((await service.ListOrdersByCustomerAsync(id, ActorProvider.GetActor(context), context.RequestAborted)).Select(OrderResponse.From)));

app.Run();

public partial class Program;

public static class ApiVersion
{
    public const string Value = "2026-11-12";
}

public sealed record Actor(string Id, IReadOnlySet<string> Permissions)
{
    public bool Has(string permission) => Permissions.Contains(permission);
}

public static class Permissions
{
    public const string CustomersCreate = "customers:create";
    public const string ProductsCreate = "products:create";
    public const string ProductsManageStock = "products:manage-stock";
    public const string OrdersCreate = "orders:create";
    public const string OrdersSubmit = "orders:submit";
    public const string OrdersApprove = "orders:approve";
    public const string OrdersShip = "orders:ship";
    public const string OrdersDeliver = "orders:deliver";
    public const string OrdersCancel = "orders:cancel";
    public const string OrdersRead = "orders:read";
    public const string OrdersReadAll = "orders:read-all";

    public static readonly string[] All =
    [
        CustomersCreate, ProductsCreate, ProductsManageStock, OrdersCreate, OrdersSubmit,
        OrdersApprove, OrdersShip, OrdersDeliver, OrdersCancel, OrdersRead, OrdersReadAll
    ];
}

public static class ActorProvider
{
    public static Actor GetActor(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Test-Actor", out var header) && !string.IsNullOrWhiteSpace(header))
        {
            var dto = JsonSerializer.Deserialize<TestActorDto>(header!, JsonSerializerOptions.Web)
                ?? throw new BadHttpRequestException("Invalid X-Test-Actor header.");
            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                throw new BadHttpRequestException("X-Test-Actor id is required.");
            }

            return new Actor(dto.Id, (dto.Permissions ?? []).ToHashSet(StringComparer.Ordinal));
        }

        var claimId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("oid")?.Value;
        var rolePermissions = context.User.FindAll("role").Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        return string.IsNullOrWhiteSpace(claimId) || rolePermissions.Count == 0
            ? new Actor("admin", Permissions.All.ToHashSet(StringComparer.Ordinal))
            : new Actor(claimId, rolePermissions);
    }

    private sealed record TestActorDto(string Id, string[]? Permissions);
}

public sealed class OrderService(OrderManagementDbContext db, TimeProvider timeProvider)
{
    public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest request, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.CustomersCreate);
        var customer = Customer.Create(request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.ShippingAddress);
        if (await db.Customers.AnyAsync(c => c.Email == customer.Email, cancellationToken))
        {
            throw new ConflictException("A customer with this email already exists.");
        }

        db.Customers.Add(customer);
        await SaveAsync(cancellationToken, "A customer with this email already exists.");
        return customer;
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.ProductsCreate);
        var product = Product.Create(request.ProductName, request.Sku, request.UnitPrice);
        if (await db.Products.AnyAsync(p => p.Sku == product.Sku, cancellationToken))
        {
            throw new ConflictException("A product with this SKU already exists.");
        }

        db.Products.Add(product);
        await SaveAsync(cancellationToken, "A product with this SKU already exists.");
        return product;
    }

    public async Task<Product> AddStockAsync(Guid productId, AddStockRequest request, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.ProductsManageStock);
        var product = await FindProductAsync(productId, cancellationToken);
        product.AddStock(request.Quantity);
        await SaveAsync(cancellationToken);
        return product;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersCreate);
        if (request.CustomerId == Guid.Empty)
        {
            throw new DomainValidationException("CustomerId is required.");
        }

        ValidateOrderLineRequests(request.LineItems);
        if (!await db.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken))
        {
            throw new NotFoundException("Customer was not found.");
        }

        var productIds = request.LineItems.Select(i => i.ProductId).ToArray();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        foreach (var id in productIds)
        {
            if (!products.ContainsKey(id))
            {
                throw new NotFoundException("Product was not found.");
            }
        }

        var order = Order.Create(request.CustomerId, actor.Id, request.LineItems.Select(i =>
        {
            var product = products[i.ProductId];
            return LineItem.Create(product.Id, product.ProductName, i.Quantity, product.UnitPrice);
        }), timeProvider.GetUtcNow());

        db.Orders.Add(order);
        await SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> AddLineItemAsync(Guid orderId, AddLineItemRequest request, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersCreate);
        if (request.ProductId == Guid.Empty)
        {
            throw new DomainValidationException("ProductId is required.");
        }

        var order = await FindOrderAsync(orderId, cancellationToken);
        var product = await FindProductAsync(request.ProductId, cancellationToken);
        order.AddLineItem(product, request.Quantity);
        await SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> RemoveLineItemAsync(Guid orderId, Guid lineItemId, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersCreate);
        var order = await FindOrderAsync(orderId, cancellationToken);
        order.RemoveLineItem(lineItemId);
        await SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> SubmitOrderAsync(Guid orderId, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersSubmit);
        var order = await FindOrderAsync(orderId, cancellationToken);
        var products = await ProductsForOrderAsync(order, cancellationToken);
        order.Submit(products, timeProvider.GetUtcNow());
        await SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> ApproveOrderAsync(Guid orderId, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersApprove);
        var order = await FindOrderAsync(orderId, cancellationToken);
        order.Approve(timeProvider.GetUtcNow());
        await SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> ShipOrderAsync(Guid orderId, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersShip);
        var order = await FindOrderAsync(orderId, cancellationToken);
        order.Ship(timeProvider.GetUtcNow());
        await SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> DeliverOrderAsync(Guid orderId, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersDeliver);
        var order = await FindOrderAsync(orderId, cancellationToken);
        order.Deliver(timeProvider.GetUtcNow());
        await SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> CancelOrderAsync(Guid orderId, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersCancel);
        var order = await FindOrderAsync(orderId, cancellationToken);
        if (order.CreatedByActorId != actor.Id && !actor.Has(Permissions.OrdersReadAll))
        {
            throw new ForbiddenException("Only the order creator or an administrator can cancel this order.");
        }

        var products = await ProductsForOrderAsync(order, cancellationToken);
        order.Cancel(products, timeProvider.GetUtcNow());
        await SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> GetOrderAsync(Guid orderId, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersRead);
        return await FindOrderAsync(orderId, cancellationToken);
    }

    public async Task<List<Order>> ListOrdersByCustomerAsync(Guid customerId, Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersReadAll);
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
        {
            throw new NotFoundException("Customer was not found.");
        }

        return await db.Orders.Include(o => o.LineItems).Where(o => o.CustomerId == customerId).ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> ListOverdueOrdersAsync(Actor actor, CancellationToken cancellationToken = default)
    {
        Require(actor, Permissions.OrdersReadAll);
        var cutoff = timeProvider.GetUtcNow().AddDays(-7);
        return await db.Orders.Include(o => o.LineItems)
            .Where(o => o.Status == OrderStatus.Submitted && o.SubmittedAt < cutoff)
            .ToListAsync(cancellationToken);
    }

    private async Task<Order> FindOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
        ?? throw new NotFoundException("Order was not found.");

    private async Task<Product> FindProductAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
        ?? throw new NotFoundException("Product was not found.");

    private async Task<Dictionary<Guid, Product>> ProductsForOrderAsync(Order order, CancellationToken cancellationToken)
    {
        var productIds = order.LineItems.Select(i => i.ProductId).ToArray();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        if (products.Count != productIds.Length)
        {
            throw new NotFoundException("Product was not found.");
        }

        return products;
    }

    private async Task SaveAsync(CancellationToken cancellationToken, string? conflictMessage = null)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (conflictMessage is not null)
        {
            throw new ConflictException(conflictMessage);
        }
    }

    private static void Require(Actor actor, string permission)
    {
        if (!actor.Has(permission))
        {
            throw new ForbiddenException($"Missing required permission: {permission}.");
        }
    }

    private static void ValidateOrderLineRequests(IReadOnlyCollection<CreateOrderLineItemRequest>? lineItems)
    {
        if (lineItems is null || lineItems.Count == 0)
        {
            throw new DomainValidationException("An order must have at least one line item.");
        }

        foreach (var item in lineItems)
        {
            if (item.ProductId == Guid.Empty)
            {
                throw new DomainValidationException("ProductId is required.");
            }

            LineItem.ValidateQuantity(item.Quantity);
        }

        if (lineItems.Select(i => i.ProductId).Distinct().Count() != lineItems.Count)
        {
            throw new DomainValidationException("The same product cannot appear in multiple line items.");
        }
    }
}

public sealed class OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, string>(
            value => value.UtcDateTime.ToString("O"),
            value => DateTimeOffset.Parse(value).ToUniversalTime());
        var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, string?>(
            value => value.HasValue ? value.Value.UtcDateTime.ToString("O") : null,
            value => value == null ? null : DateTimeOffset.Parse(value).ToUniversalTime());

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Email).IsUnique();
            entity.OwnsOne(c => c.ShippingAddress, owned =>
            {
                owned.Property(a => a.Street).IsRequired();
                owned.Property(a => a.City).IsRequired();
                owned.Property(a => a.State).IsRequired();
                owned.Property(a => a.PostalCode).IsRequired();
                owned.Property(a => a.Country).IsRequired();
            });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.CustomerId);
            entity.HasIndex(o => new { o.Status, o.SubmittedAt });
            entity.Property(o => o.Status).HasConversion(new EnumToStringConverter<OrderStatus>()).HasMaxLength(32);
            entity.Property(o => o.CreatedAt).HasConversion(dateTimeOffsetConverter);
            entity.Property(o => o.SubmittedAt).HasConversion(nullableDateTimeOffsetConverter);
            entity.Property(o => o.ShippedAt).HasConversion(nullableDateTimeOffsetConverter);
            entity.HasMany(o => o.LineItems).WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(o => o.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<LineItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
        });
    }
}

public enum OrderStatus
{
    Draft,
    Submitted,
    Approved,
    Shipped,
    Delivered,
    Cancelled
}

public sealed class Customer
{
    private Customer() { }

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    public static Customer Create(string firstName, string lastName, string email, string? phoneNumber, ShippingAddress shippingAddress)
    {
        ValidateRequiredLength(firstName, nameof(firstName), 100);
        ValidateRequiredLength(lastName, nameof(lastName), 100);
        ValidateEmail(email);
        if (!string.IsNullOrWhiteSpace(phoneNumber) && !Regex.IsMatch(phoneNumber, @"^\+?[0-9 .()\-]{7,25}$"))
        {
            throw new DomainValidationException("PhoneNumber is invalid.");
        }

        ShippingAddress.Validate(shippingAddress);
        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            ShippingAddress = shippingAddress.Normalized()
        };
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainValidationException("Email is required.");
        }

        try
        {
            _ = new MailAddress(email);
        }
        catch (FormatException)
        {
            throw new DomainValidationException("Email is invalid.");
        }
    }

    private static void ValidateRequiredLength(string value, string fieldName, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        if (value.Trim().Length > max)
        {
            throw new DomainValidationException($"{fieldName} must be {max} characters or fewer.");
        }
    }
}

public sealed record ShippingAddress(string Street, string City, string State, string PostalCode, string Country)
{
    public static void Validate(ShippingAddress? address)
    {
        if (address is null)
        {
            throw new DomainValidationException("ShippingAddress is required.");
        }

        if (new[] { address.Street, address.City, address.State, address.PostalCode, address.Country }.Any(string.IsNullOrWhiteSpace))
        {
            throw new DomainValidationException("All shipping address fields are required.");
        }
    }

    public ShippingAddress Normalized() => new(Street.Trim(), City.Trim(), State.Trim(), PostalCode.Trim(), Country.Trim());
}

public sealed class Product
{
    private Product() { }
    private static readonly Regex SkuPattern = new("^[A-Z0-9]{3,20}$", RegexOptions.Compiled);

    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = "";
    public string Sku { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    public static Product Create(string productName, string sku, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName) || productName.Trim().Length > 200)
        {
            throw new DomainValidationException("ProductName is required and must be 200 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(sku) || !SkuPattern.IsMatch(sku))
        {
            throw new DomainValidationException("SKU must be 3-20 uppercase letters and digits.");
        }

        if (unitPrice <= 0)
        {
            throw new DomainValidationException("UnitPrice must be greater than zero.");
        }

        return new Product
        {
            Id = Guid.NewGuid(),
            ProductName = productName.Trim(),
            Sku = sku,
            UnitPrice = unitPrice,
            StockQuantity = 0
        };
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Stock addition quantity must be positive.");
        }

        StockQuantity += quantity;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Reserved quantity must be positive.");
        }

        if (StockQuantity < quantity)
        {
            throw new DomainValidationException("Insufficient stock.");
        }

        StockQuantity -= quantity;
    }

    public void ReleaseStock(int quantity) => StockQuantity += quantity;
}

public sealed class Order
{
    private readonly List<LineItem> _lineItems = [];

    private Order() { }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CreatedByActorId { get; private set; } = "";
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public IReadOnlyCollection<LineItem> LineItems => _lineItems;
    public decimal Total => _lineItems.Sum(i => i.UnitPrice * i.Quantity);

    public static Order Create(Guid customerId, string createdByActorId, IEnumerable<LineItem> lineItems, DateTimeOffset createdAt)
    {
        var items = lineItems.ToList();
        if (customerId == Guid.Empty)
        {
            throw new DomainValidationException("CustomerId is required.");
        }

        if (string.IsNullOrWhiteSpace(createdByActorId))
        {
            throw new DomainValidationException("CreatedByActorId is required.");
        }

        if (items.Count == 0)
        {
            throw new DomainValidationException("An order must have at least one line item.");
        }

        if (items.Select(i => i.ProductId).Distinct().Count() != items.Count)
        {
            throw new DomainValidationException("The same product cannot appear in multiple line items.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedByActorId = createdByActorId,
            Status = OrderStatus.Draft,
            CreatedAt = createdAt
        };
        order._lineItems.AddRange(items);
        return order;
    }

    public void AddLineItem(Product product, int quantity)
    {
        EnsureDraft();
        LineItem.ValidateQuantity(quantity);
        if (_lineItems.Any(i => i.ProductId == product.Id))
        {
            throw new DomainValidationException("The product already exists in this order.");
        }

        _lineItems.Add(LineItem.Create(product.Id, product.ProductName, quantity, product.UnitPrice));
    }

    public void RemoveLineItem(Guid lineItemId)
    {
        EnsureDraft();
        if (_lineItems.Count == 1)
        {
            throw new DomainValidationException("Cannot remove the last line item from an order.");
        }

        var item = _lineItems.FirstOrDefault(i => i.Id == lineItemId)
            ?? throw new NotFoundException("Line item was not found.");
        _lineItems.Remove(item);
    }

    public void Submit(IReadOnlyDictionary<Guid, Product> products, DateTimeOffset submittedAt)
    {
        EnsureStatus(OrderStatus.Draft, "Only Draft orders can be submitted.");
        if (_lineItems.Count == 0)
        {
            throw new DomainValidationException("An order must have at least one line item.");
        }

        foreach (var item in _lineItems)
        {
            products[item.ProductId].ReserveStock(item.Quantity);
        }

        Status = OrderStatus.Submitted;
        SubmittedAt = submittedAt;
    }

    public void Approve(DateTimeOffset approvedAt)
    {
        EnsureStatus(OrderStatus.Submitted, "Only Submitted orders can be approved.");
        Status = OrderStatus.Approved;
    }

    public void Ship(DateTimeOffset shippedAt)
    {
        EnsureStatus(OrderStatus.Approved, "Only Approved orders can be shipped.");
        Status = OrderStatus.Shipped;
        ShippedAt = shippedAt;
    }

    public void Deliver(DateTimeOffset deliveredAt)
    {
        EnsureStatus(OrderStatus.Shipped, "Only Shipped orders can be delivered.");
        Status = OrderStatus.Delivered;
    }

    public void Cancel(IReadOnlyDictionary<Guid, Product> products, DateTimeOffset cancelledAt)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            throw new DomainValidationException($"Orders in {Status} status cannot be cancelled.");
        }

        if (Status is OrderStatus.Submitted or OrderStatus.Approved)
        {
            foreach (var item in _lineItems)
            {
                products[item.ProductId].ReleaseStock(item.Quantity);
            }
        }

        Status = OrderStatus.Cancelled;
    }

    public bool IsOverdue(DateTimeOffset now) =>
        Status == OrderStatus.Submitted && SubmittedAt is not null && SubmittedAt.Value < now.AddDays(-7);

    private void EnsureDraft() => EnsureStatus(OrderStatus.Draft, "Only Draft orders can be modified.");

    private void EnsureStatus(OrderStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainValidationException(message);
        }
    }
}

public sealed class LineItem
{
    private LineItem() { }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public static LineItem Create(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainValidationException("ProductId is required.");
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainValidationException("ProductName is required.");
        }

        ValidateQuantity(quantity);
        if (unitPrice <= 0)
        {
            throw new DomainValidationException("UnitPrice must be greater than zero.");
        }

        return new LineItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    public static void ValidateQuantity(int quantity)
    {
        if (quantity is < 1 or > 999)
        {
            throw new DomainValidationException("Line item quantity must be between 1 and 999.");
        }
    }
}

public sealed record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ShippingAddress ShippingAddress);

public sealed record CreateProductRequest(string ProductName, string Sku, decimal UnitPrice);
public sealed record AddStockRequest(int Quantity);
public sealed record CreateOrderLineItemRequest(Guid ProductId, int Quantity);
public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyCollection<CreateOrderLineItemRequest> LineItems);
public sealed record AddLineItemRequest(Guid ProductId, int Quantity);

public sealed record CustomerResponse(Guid Id, string FirstName, string LastName, string Email, string? PhoneNumber, ShippingAddress ShippingAddress)
{
    public static CustomerResponse From(Customer customer) =>
        new(customer.Id, customer.FirstName, customer.LastName, customer.Email, customer.PhoneNumber, customer.ShippingAddress);
}

public sealed record ProductResponse(Guid Id, string ProductName, string Sku, decimal UnitPrice, int StockQuantity)
{
    public static ProductResponse From(Product product) =>
        new(product.Id, product.ProductName, product.Sku, product.UnitPrice, product.StockQuantity);
}

public sealed record LineItemResponse(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal)
{
    public static LineItemResponse From(LineItem item) =>
        new(item.Id, item.ProductId, item.ProductName, item.Quantity, item.UnitPrice, item.UnitPrice * item.Quantity);
}

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ShippedAt,
    decimal Total,
    IReadOnlyCollection<LineItemResponse> LineItems)
{
    public static OrderResponse From(Order order) =>
        new(order.Id, order.CustomerId, order.CreatedByActorId, order.Status, order.CreatedAt, order.SubmittedAt,
            order.ShippedAt, order.Total, order.LineItems.Select(LineItemResponse.From).ToList());
}

public sealed class DomainValidationException(string message) : Exception(message);
public sealed class NotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class ForbiddenException(string message) : Exception(message);
