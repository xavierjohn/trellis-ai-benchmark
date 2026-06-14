namespace OrderManagement.Application.Orders;

using OrderManagement.Domain;

/// <summary>A single requested line when creating a draft order or adding a line item.</summary>
public sealed record OrderLineInput(ProductId ProductId, Quantity Quantity);
