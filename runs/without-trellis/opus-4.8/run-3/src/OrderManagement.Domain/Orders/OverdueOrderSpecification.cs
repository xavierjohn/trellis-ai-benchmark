namespace OrderManagement.Domain.Orders;

/// <summary>
/// An order is overdue when it has been in <see cref="OrderStatus.Submitted"/> status
/// for more than 7 days without being approved.
/// </summary>
public static class OverdueOrderSpecification
{
    public static readonly TimeSpan Threshold = TimeSpan.FromDays(7);

    public static bool IsOverdue(Order order, DateTimeOffset now)
        => order.Status == OrderStatus.Submitted
           && order.SubmittedAt is { } submittedAt
           && now - submittedAt > Threshold;
}
