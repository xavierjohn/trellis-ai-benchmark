namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>Matches submitted orders older than seven days.</summary>
public sealed class OverdueOrderSpecification : Specification<Order>
{
    private readonly DateTimeOffset _asOf;

    /// <summary>Constructor.</summary>
    public OverdueOrderSpecification(DateTimeOffset asOf) => _asOf = asOf;

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression() =>
        order => order.Status == OrderStatus.Submitted &&
                 order.SubmittedAt.HasValue &&
                 order.SubmittedAt.Value < _asOf.AddDays(-7);
}
