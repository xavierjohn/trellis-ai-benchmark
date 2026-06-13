namespace OrderManagement.Domain.Orders;

/// <summary>
/// An order is overdue if it has been in Submitted status for more than 7 days
/// without being Approved.
/// </summary>
public static class OverdueOrderSpecification
{
    public static readonly TimeSpan Threshold = TimeSpan.FromDays(7);

    public static bool IsOverdue(Order order, DateTimeOffset now) =>
        order.Status == OrderStatus.Submitted
        && order.SubmittedAt.HasValue
        && now - order.SubmittedAt.Value > Threshold;

    /// <summary>The latest SubmittedAt value that still counts as overdue at <paramref name="now"/>.</summary>
    public static DateTimeOffset SubmittedBefore(DateTimeOffset now) => now - Threshold;
}
