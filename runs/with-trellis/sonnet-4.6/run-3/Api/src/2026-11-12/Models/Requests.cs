namespace OrderManagement.Api.v2026_11_12.Models;

/// <summary>Request to create a customer.</summary>
public record CreateCustomerRequest
{
    /// <summary>First name (1-100 chars).</summary>
    public string? FirstName { get; init; }

    /// <summary>Last name (1-100 chars).</summary>
    public string? LastName { get; init; }

    /// <summary>Email address.</summary>
    public string? Email { get; init; }

    /// <summary>Phone number (optional).</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Shipping address.</summary>
    public ShippingAddressRequest? ShippingAddress { get; init; }
}

/// <summary>Shipping address request.</summary>
public record ShippingAddressRequest
{
    /// <summary>Street address.</summary>
    public string? Street { get; init; }

    /// <summary>City.</summary>
    public string? City { get; init; }

    /// <summary>State or province.</summary>
    public string? State { get; init; }

    /// <summary>Postal code.</summary>
    public string? PostalCode { get; init; }

    /// <summary>Country.</summary>
    public string? Country { get; init; }
}

/// <summary>Request to create a product.</summary>
public record CreateProductRequest
{
    /// <summary>Product name (1-200 chars).</summary>
    public string? ProductName { get; init; }

    /// <summary>SKU (3-20 chars, uppercase alphanumeric).</summary>
    public string? Sku { get; init; }

    /// <summary>Unit price (must be positive).</summary>
    public decimal UnitPrice { get; init; }
}

/// <summary>Request to add stock to a product.</summary>
public record AddStockRequest
{
    /// <summary>Quantity to add (must be positive).</summary>
    public int Quantity { get; init; }
}

/// <summary>Request to create a draft order.</summary>
public record CreateDraftOrderRequest
{
    /// <summary>Customer ID.</summary>
    public Guid CustomerId { get; init; }

    /// <summary>Line items.</summary>
    public IReadOnlyList<LineItemRequest> LineItems { get; init; } = [];
}

/// <summary>Line item request.</summary>
public record LineItemRequest
{
    /// <summary>Product ID.</summary>
    public Guid ProductId { get; init; }

    /// <summary>Quantity (1-999).</summary>
    public int Quantity { get; init; }
}

/// <summary>Request to add a line item to a draft order.</summary>
public record AddLineItemRequest
{
    /// <summary>Product ID.</summary>
    public Guid ProductId { get; init; }

    /// <summary>Quantity (1-999).</summary>
    public int Quantity { get; init; }
}
