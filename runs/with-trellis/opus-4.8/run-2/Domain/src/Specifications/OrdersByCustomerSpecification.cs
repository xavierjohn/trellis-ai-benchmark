namespace OrderManagement.Domain;

using System.Linq.Expressions;

/// <summary>
/// Matches all orders belonging to a given customer.
/// </summary>
public sealed class OrdersByCustomerSpecification : Specification<Order>
{
    private readonly CustomerId _customerId;

    /// <summary>Creates the specification for the given customer.</summary>
    public OrdersByCustomerSpecification(CustomerId customerId) => _customerId = customerId;

    /// <inheritdoc />
    public override Expression<Func<Order, bool>> ToExpression() =>
        order => order.CustomerId == _customerId;
}
