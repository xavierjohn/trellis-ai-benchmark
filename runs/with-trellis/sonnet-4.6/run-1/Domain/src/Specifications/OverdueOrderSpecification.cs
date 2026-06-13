namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>
/// Matches submitted orders that have been awaiting approval for more than seven days.
/// </summary>
public class OverdueOrderSpecification : Specification<Order>
{
    private readonly DateTime _cutoff;

    /// <summary>
    /// Creates the specification relative to the supplied instant.
    /// </summary>
    public OverdueOrderSpecification(DateTime asOf) => _cutoff = asOf.AddDays(-7);

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression() =>
        order => order.Status == OrderStatus.Submitted && order.SubmittedAt.GetValueOrDefault(DateTime.MaxValue) < _cutoff;
}
