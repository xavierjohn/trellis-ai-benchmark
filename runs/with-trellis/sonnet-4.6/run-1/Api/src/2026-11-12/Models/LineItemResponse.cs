namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Order line item response model.</summary>
public sealed record LineItemResponse
{
    /// <summary>Line item identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Product identifier.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Snapshot product name.</summary>
    public string ProductName { get; init; } = null!;
    /// <summary>Quantity.</summary>
    public int Quantity { get; init; }
    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; init; }
    /// <summary>Total line amount.</summary>
    public decimal Total { get; init; }

    /// <summary>Maps from the domain entity.</summary>
    public static LineItemResponse From(LineItem lineItem) => new()
    {
        Id = lineItem.Id.Value,
        ProductId = lineItem.ProductId.Value,
        ProductName = lineItem.ProductName,
        Quantity = lineItem.Quantity,
        UnitPrice = lineItem.UnitPrice,
        Total = lineItem.Total,
    };
}
