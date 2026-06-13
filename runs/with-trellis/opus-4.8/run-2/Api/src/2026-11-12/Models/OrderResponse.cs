namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Response body for a line item.</summary>
/// <param name="LineItemId">Line item id.</param>
/// <param name="ProductId">Product id.</param>
/// <param name="ProductName">Product name snapshot.</param>
/// <param name="Quantity">Quantity ordered.</param>
/// <param name="UnitPrice">Unit price snapshot.</param>
/// <param name="LineTotal">Line total (unit price × quantity).</param>
public sealed record LineItemResponse(
    Guid LineItemId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal)
{
    /// <summary>Projects a domain line item to its response representation.</summary>
    public static LineItemResponse From(LineItem lineItem) =>
        new(
            lineItem.Id.Value,
            lineItem.ProductId.Value,
            lineItem.ProductName.Value,
            lineItem.Quantity.Value,
            lineItem.UnitPrice.Value,
            lineItem.LineTotal.Value);
}

/// <summary>Response body for an order.</summary>
/// <param name="Id">Order id.</param>
/// <param name="CustomerId">Customer id.</param>
/// <param name="CreatedByActorId">Identity of the actor who created the order.</param>
/// <param name="Status">Current status.</param>
/// <param name="Total">Order total.</param>
/// <param name="CreatedAt">When the order was created.</param>
/// <param name="SubmittedAt">When the order was submitted, if it has been.</param>
/// <param name="ShippedAt">When the order was shipped, if it has been.</param>
/// <param name="LineItems">The line items.</param>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ShippedAt,
    IReadOnlyList<LineItemResponse> LineItems)
{
    /// <summary>Projects a domain order to its response representation.</summary>
    public static OrderResponse From(Order order) =>
        new(
            order.Id.Value,
            order.CustomerId.Value,
            order.CreatedByActorId,
            order.Status.Value,
            order.Total.Value,
            order.CreatedAt,
            order.SubmittedAt.HasValue ? order.SubmittedAt.Value : null,
            order.ShippedAt.HasValue ? order.ShippedAt.Value : null,
            order.LineItems.Select(LineItemResponse.From).ToArray());
}
