namespace Api.Tests._2026_11_12;

public class CustomerTestResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public ShippingAddressTestResponse ShippingAddress { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastModified { get; set; }
}

public class ShippingAddressTestResponse
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;
}

public class ProductTestResponse
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastModified { get; set; }
}

public class OrderTestResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CreatedByActorId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public List<LineItemTestResponse> LineItems { get; set; } = [];
    public decimal OrderTotal { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastModified { get; set; }
}

public class LineItemTestResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
