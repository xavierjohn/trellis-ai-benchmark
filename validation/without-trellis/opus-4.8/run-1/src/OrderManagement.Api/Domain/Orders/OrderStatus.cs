namespace OrderManagement.Api.Domain.Orders;

public enum OrderStatus
{
    Draft,
    Submitted,
    Approved,
    Shipped,
    Delivered,
    Cancelled
}
