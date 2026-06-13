namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>Specification for orders submitted more than seven days ago and not approved.</summary>
public sealed class OverdueOrderSpecification : Specification<Order>
{
    private readonly DateTime _asOf;

    /// <summary>Create the specification.</summary>
    public OverdueOrderSpecification(DateTime asOf) => _asOf = asOf;

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression()
    {
        var cutoff = _asOf.AddDays(-7);
        return order => order.Status == OrderStatus.Submitted &&
            order.SubmittedAt.HasValue &&
            order.SubmittedAt.Value < cutoff;
    }
}
