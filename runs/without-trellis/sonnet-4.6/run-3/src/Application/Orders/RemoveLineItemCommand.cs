namespace Application.Orders;

public record RemoveLineItemCommand(Guid OrderId, Guid LineItemId);
