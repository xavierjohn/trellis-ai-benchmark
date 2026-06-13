namespace OrderManagement.Application.Orders;

using OrderManagement.Domain;

public sealed record DraftOrderLine(ProductId ProductId, LineItemQuantity Quantity);
