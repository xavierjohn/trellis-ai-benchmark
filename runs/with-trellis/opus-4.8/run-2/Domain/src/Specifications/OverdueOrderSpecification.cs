namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>
/// Matches orders that are overdue: still in Submitted status and submitted more than
/// 7 days before the supplied reference time.
/// </summary>
public sealed class OverdueOrderSpecification : Specification<Order>
{
    private readonly DateTime _cutoff;

    /// <summary>
    /// Creates the specification relative to <paramref name="asOf"/>. Orders submitted on or
    /// before <c>asOf - 7 days</c> and still Submitted are overdue.
    /// </summary>
    public OverdueOrderSpecification(DateTime asOf) => _cutoff = asOf.AddDays(-7);

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression() =>
        order => order.Status == OrderStatus.Submitted
                 && order.SubmittedAt.HasValue
                 && order.SubmittedAt.Value < _cutoff;
}
