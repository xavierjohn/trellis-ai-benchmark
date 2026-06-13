namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>
/// Specification for overdue orders: submitted more than 7 days ago.
/// </summary>
public class OverdueOrderSpecification : Specification<Order>
{
    private readonly DateTime _cutoff;

    /// <summary>Initializes with the time cutoff derived from the given time provider.</summary>
    public OverdueOrderSpecification(TimeProvider timeProvider) =>
        _cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression() =>
        order => order.Status == OrderStatus.Submitted &&
                 order.SubmittedAt.GetValueOrDefault(DateTime.MaxValue) < _cutoff;
}
