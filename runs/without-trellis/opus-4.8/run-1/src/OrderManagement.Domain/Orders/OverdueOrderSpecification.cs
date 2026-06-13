namespace OrderManagement.Domain.Orders;

/// <summary>
/// Specification for overdue orders: in Submitted status for more than 7 days without being approved.
/// </summary>
public static class OverdueOrderSpecification
{
    public static readonly TimeSpan Threshold = TimeSpan.FromDays(7);

    public static bool IsOverdue(Order order, DateTimeOffset now)
        => order.Status == OrderStatus.Submitted
           && order.SubmittedAt is { } submittedAt
           && now - submittedAt > Threshold;
}
