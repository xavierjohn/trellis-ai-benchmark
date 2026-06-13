namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Request body for creating a customer.</summary>
public record CreateCustomerRequest
{
    /// <summary>Customer's first name.</summary>
    public FirstName FirstName { get; init; } = null!;

    /// <summary>Customer's last name.</summary>
    public LastName LastName { get; init; } = null!;

    /// <summary>Customer's email address.</summary>
    public EmailAddress Email { get; init; } = null!;

    /// <summary>Customer's optional phone number (raw string).</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Customer's shipping address.</summary>
    public ShippingAddressRequest ShippingAddress { get; init; } = null!;
}

/// <summary>Shipping address request data.</summary>
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

/// <summary>Request body for creating a product.</summary>
public record CreateProductRequest
{
    /// <summary>Product name.</summary>
    public ProductName ProductName { get; init; } = null!;

    /// <summary>Stock-keeping unit.</summary>
    public SKU SKU { get; init; } = null!;

    /// <summary>Unit price.</summary>
    public UnitPrice UnitPrice { get; init; } = null!;
}

/// <summary>Request body for adding stock to a product.</summary>
public record AddStockRequest
{
    /// <summary>Quantity to add.</summary>
    public Quantity Quantity { get; init; } = null!;
}

/// <summary>Request body for creating a draft order.</summary>
public record CreateDraftOrderRequest
{
    /// <summary>Customer placing the order.</summary>
    public CustomerId CustomerId { get; init; } = null!;

    /// <summary>Line items in the order.</summary>
    public IReadOnlyList<OrderLineItemRequest> LineItems { get; init; } = [];
}

/// <summary>A line item in a create order request.</summary>
public record OrderLineItemRequest
{
    /// <summary>Product ID.</summary>
    public ProductId ProductId { get; init; } = null!;

    /// <summary>Quantity to order.</summary>
    public Quantity Quantity { get; init; } = null!;
}

/// <summary>Request body for adding a line item to an order.</summary>
public record AddLineItemRequest
{
    /// <summary>Product ID.</summary>
    public ProductId ProductId { get; init; } = null!;

    /// <summary>Quantity to order.</summary>
    public Quantity Quantity { get; init; } = null!;
}
