namespace OrderManagement.Api.Domain.Orders;

/// <summary>
/// An order is overdue if it has been in Submitted status for more than 7 days
/// without being approved.
/// </summary>
public static class OverdueOrderSpecification
{
    public const int OverdueDays = 7;

    public static bool IsOverdue(Order order, DateTime utcNow)
    {
        if (order.Status != OrderStatus.Submitted || order.SubmittedAt is null)
            return false;

        return order.SubmittedAt.Value < utcNow.AddDays(-OverdueDays);
    }
}
