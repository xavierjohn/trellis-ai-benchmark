namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>
/// Matches submitted orders that have been waiting for approval beyond the cutoff.
/// </summary>
public class OverdueOrderSpecification : Specification<Order>
{
    private readonly DateTime _cutoff;

    /// <summary>
    /// Creates the specification from a cutoff timestamp.
    /// </summary>
    public OverdueOrderSpecification(DateTime cutoff) => _cutoff = cutoff;

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression() =>
        order => order.Status == OrderStatus.Submitted &&
                 order.SubmittedAt.HasValue &&
                 order.SubmittedAt.Value < _cutoff;
}
