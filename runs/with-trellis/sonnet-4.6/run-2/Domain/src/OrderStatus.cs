namespace OrderManagement.Domain;

/// <summary>
/// Lifecycle status of an order.
/// </summary>
public partial class OrderStatus : RequiredEnum<OrderStatus>
{
    /// <summary>Order is being drafted.</summary>
    public static readonly OrderStatus Draft = new();

    /// <summary>Order has been submitted for approval.</summary>
    public static readonly OrderStatus Submitted = new();

    /// <summary>Order has been approved.</summary>
    public static readonly OrderStatus Approved = new();

    /// <summary>Order has been shipped.</summary>
    public static readonly OrderStatus Shipped = new();

    /// <summary>Order has been delivered.</summary>
    public static readonly OrderStatus Delivered = new();

    /// <summary>Order has been cancelled.</summary>
    public static readonly OrderStatus Cancelled = new();
}
