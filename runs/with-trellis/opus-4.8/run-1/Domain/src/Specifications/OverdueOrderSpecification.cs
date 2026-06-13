namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>
/// Matches orders that have been in Submitted status for more than 7 days without being approved.
/// </summary>
public sealed class OverdueOrderSpecification(DateTime asOf) : Specification<Order>
{
    private readonly DateTime _threshold = asOf.AddDays(-7);

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression() =>
        o => o.Status == OrderStatus.Submitted
             && o.SubmittedAt.HasValue
             && o.SubmittedAt.Value < _threshold;
}
