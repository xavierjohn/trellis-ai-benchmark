namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>
/// Matches orders submitted more than seven days ago and not yet approved.
/// </summary>
public sealed class OverdueOrderSpecification : Specification<Order>
{
    private readonly DateTime _asOfUtc;

    /// <summary>Constructor.</summary>
    public OverdueOrderSpecification(DateTime asOfUtc) => _asOfUtc = asOfUtc;

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression() =>
        order => order.Status == OrderStatus.Submitted &&
                 order.SubmittedAt.HasValue &&
                 order.SubmittedAt.Value < _asOfUtc.AddDays(-7);
}
