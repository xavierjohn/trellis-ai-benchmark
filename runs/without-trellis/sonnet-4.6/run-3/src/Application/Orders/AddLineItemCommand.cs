namespace Application.Orders;

public record AddLineItemCommand(Guid OrderId, Guid ProductId, int Quantity);
