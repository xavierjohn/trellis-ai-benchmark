namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>
/// Matches orders that have been in Submitted status for more than 7 days without being approved.
/// </summary>
public sealed class OverdueOrderSpecification : Specification<Order>
{
    private readonly DateTime _threshold;

    /// <summary>Creates the specification using the supplied clock (overdue = submitted &gt; 7 days ago).</summary>
    public OverdueOrderSpecification(TimeProvider timeProvider) =>
        _threshold = timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression() =>
        order => order.Status == OrderStatus.Submitted
            && order.SubmittedAt.GetValueOrDefault(DateTime.MaxValue) < _threshold;
}
