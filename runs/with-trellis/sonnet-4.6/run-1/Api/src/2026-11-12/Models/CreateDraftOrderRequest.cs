namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Create order request body.</summary>
public sealed record CreateDraftOrderRequest
{
    /// <summary>Customer identifier.</summary>
    public CustomerId CustomerId { get; init; } = null!;
    /// <summary>Initial line items.</summary>
    public IReadOnlyList<CreateOrderLineItemRequest> LineItems { get; init; } = [];
}
