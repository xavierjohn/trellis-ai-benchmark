namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>
/// Matches orders that are overdue: in Submitted status with SubmittedAt older than 7 days.
/// </summary>
public class OverdueOrderSpecification : Specification<Order>
{
    private readonly DateTime _cutoff;

    public OverdueOrderSpecification(TimeProvider timeProvider)
    {
        _cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);
    }

    public OverdueOrderSpecification(DateTime cutoff)
    {
        _cutoff = cutoff;
    }

    public override Expression<Func<Order, bool>> ToExpression() =>
        order => order.Status == OrderStatus.Submitted
            && order.SubmittedAt.HasValue
            && order.SubmittedAt.GetValueOrDefault(DateTime.MinValue) < _cutoff;
}
