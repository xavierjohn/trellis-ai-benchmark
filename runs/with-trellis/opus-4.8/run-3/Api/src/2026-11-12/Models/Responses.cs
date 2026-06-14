namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Response model for a customer.</summary>
public sealed record CustomerResponse
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>First name.</summary>
    public string FirstName { get; init; } = null!;

    /// <summary>Last name.</summary>
    public string LastName { get; init; } = null!;

    /// <summary>Email address.</summary>
    public string Email { get; init; } = null!;

    /// <summary>Phone number, if provided.</summary>
    public string? Phone { get; init; }

    /// <summary>Shipping address.</summary>
    public ShippingAddress ShippingAddress { get; init; } = null!;

    /// <summary>Maps a domain customer to its response representation.</summary>
    public static CustomerResponse From(Customer customer) => new()
    {
        Id = customer.Id.Value,
        FirstName = customer.FirstName.Value,
        LastName = customer.LastName.Value,
        Email = customer.Email.Value,
        Phone = customer.Phone.Match<string?>(p => p.Value, () => null),
        ShippingAddress = customer.ShippingAddress,
    };
}

/// <summary>Response model for a product.</summary>
public sealed record ProductResponse
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Product name.</summary>
    public string Name { get; init; } = null!;

    /// <summary>Stock keeping unit.</summary>
    public string Sku { get; init; } = null!;

    /// <summary>Unit price (USD).</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Current available stock quantity.</summary>
    public int StockQuantity { get; init; }

    /// <summary>Maps a domain product to its response representation.</summary>
    public static ProductResponse From(Product product) => new()
    {
        Id = product.Id.Value,
        Name = product.Name.Value,
        Sku = product.Sku.Value,
        UnitPrice = product.UnitPrice.Value,
        StockQuantity = product.StockQuantity.Value,
    };
}

/// <summary>Response model for a single line item.</summary>
public sealed record LineItemResponse
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Referenced product identifier.</summary>
    public Guid ProductId { get; init; }

    /// <summary>Snapshot of the product name at order time.</summary>
    public string ProductName { get; init; } = null!;

    /// <summary>Quantity ordered.</summary>
    public int Quantity { get; init; }

    /// <summary>Snapshot of the unit price at order time.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>The line total (unit price × quantity).</summary>
    public decimal LineTotal { get; init; }

    /// <summary>Maps a domain line item to its response representation.</summary>
    public static LineItemResponse From(LineItem lineItem) => new()
    {
        Id = lineItem.Id.Value,
        ProductId = lineItem.ProductId.Value,
        ProductName = lineItem.ProductName.Value,
        Quantity = lineItem.Quantity.Value,
        UnitPrice = lineItem.UnitPrice.Value,
        LineTotal = lineItem.LineTotal,
    };
}

/// <summary>Response model for an order.</summary>
public sealed record OrderResponse
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Owning customer identifier.</summary>
    public Guid CustomerId { get; init; }

    /// <summary>Identity of the actor who created the order.</summary>
    public string CreatedByActorId { get; init; } = null!;

    /// <summary>Current order status.</summary>
    public string Status { get; init; } = null!;

    /// <summary>The line items in the order.</summary>
    public IReadOnlyList<LineItemResponse> LineItems { get; init; } = [];

    /// <summary>The order total.</summary>
    public decimal OrderTotal { get; init; }

    /// <summary>When the order was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the order was submitted, if applicable.</summary>
    public DateTime? SubmittedAt { get; init; }

    /// <summary>When the order was shipped, if applicable.</summary>
    public DateTime? ShippedAt { get; init; }

    /// <summary>Maps a domain order to its response representation.</summary>
    public static OrderResponse From(Order order) => new()
    {
        Id = order.Id.Value,
        CustomerId = order.CustomerId.Value,
        CreatedByActorId = order.CreatedByActorId,
        Status = order.Status.Value,
        LineItems = order.LineItems.Select(LineItemResponse.From).ToList(),
        OrderTotal = order.OrderTotal,
        CreatedAt = order.CreatedAt,
        SubmittedAt = order.SubmittedAt.AsNullable(),
        ShippedAt = order.ShippedAt.AsNullable(),
    };
}
