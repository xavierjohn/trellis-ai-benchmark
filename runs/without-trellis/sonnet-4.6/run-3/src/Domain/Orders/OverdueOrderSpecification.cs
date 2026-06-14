namespace Domain.Orders;

public static class OverdueOrderSpecification
{
    public static bool IsSatisfiedBy(Order order, TimeProvider timeProvider)
    {
        if (order.Status != OrderStatus.Submitted)
        {
            return false;
        }

        if (!order.SubmittedAt.HasValue)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return (now - order.SubmittedAt.Value).TotalDays > 7;
    }
}
