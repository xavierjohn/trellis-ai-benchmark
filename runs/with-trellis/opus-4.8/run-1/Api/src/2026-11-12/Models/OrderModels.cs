namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Application.Orders;
using OrderManagement.Domain;

/// <summary>Request body for creating a draft order.</summary>
/// <param name="CustomerId">Customer the order belongs to.</param>
/// <param name="Lines">Line items (product + quantity); at least one required.</param>
public sealed record CreateOrderRequest(CustomerId CustomerId, IReadOnlyList<OrderLineRequest> Lines);

/// <summary>Request body for adding a line item to a draft order.</summary>
/// <param name="ProductId">Product to add.</param>
/// <param name="Quantity">Quantity (1–999).</param>
public sealed record AddLineItemRequest(ProductId ProductId, Quantity Quantity);

/// <summary>Response body representing an order line item.</summary>
/// <param name="LineItemId">Line item identifier.</param>
/// <param name="ProductId">Referenced product.</param>
/// <param name="ProductName">Snapshot of product name at ordering time.</param>
/// <param name="Quantity">Quantity ordered.</param>
/// <param name="UnitPrice">Snapshot of unit price at ordering time.</param>
/// <param name="LineTotal">Unit price multiplied by quantity.</param>
public sealed record LineItemResponse(
    Guid LineItemId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal)
{
    /// <summary>Projects a domain <see cref="LineItem"/> to a response.</summary>
    public static LineItemResponse From(LineItem item) =>
        new(
            item.Id.Value,
            item.ProductId.Value,
            item.ProductName.Value,
            item.Quantity.Value,
            item.UnitPrice.Value,
            item.LineTotal);
}

/// <summary>Response body representing an order.</summary>
/// <param name="Id">Order identifier.</param>
/// <param name="CustomerId">Customer the order belongs to.</param>
/// <param name="CreatedByActorId">Identity of the actor that created the order.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="OrderTotal">Sum of all line totals.</param>
/// <param name="CreatedAt">UTC timestamp when the order was created.</param>
/// <param name="SubmittedAt">UTC timestamp when the order was submitted, when present.</param>
/// <param name="ShippedAt">UTC timestamp when the order was shipped, when present.</param>
/// <param name="LineItems">The order line items.</param>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    decimal OrderTotal,
    DateTimeOffset CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ShippedAt,
    IReadOnlyList<LineItemResponse> LineItems)
{
    /// <summary>Projects a domain <see cref="Order"/> to a response.</summary>
    public static OrderResponse From(Order order) =>
        new(
            order.Id.Value,
            order.CustomerId.Value,
            order.CreatedByActorId,
            order.Status.Value,
            order.OrderTotal,
            order.CreatedAt,
            order.SubmittedAt.TryGetValue(out var submitted) ? submitted : null,
            order.ShippedAt.TryGetValue(out var shipped) ? shipped : null,
            order.LineItems.Select(LineItemResponse.From).ToList());
}
