namespace OrderManagement.Application.Orders;

using OrderManagement.Domain;

/// <summary>A requested line item when creating a draft order.</summary>
public sealed record OrderLineRequest(ProductId ProductId, Quantity Quantity);
