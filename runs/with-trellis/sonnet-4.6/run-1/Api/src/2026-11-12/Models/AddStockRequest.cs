namespace OrderManagement.Api.v2026_11_12.Models;

/// <summary>Add stock request body.</summary>
public sealed record AddStockRequest
{
    /// <summary>Quantity to add.</summary>
    public int Quantity { get; init; }
}
