namespace OrderManagement.Domain;

/// <summary>The lifecycle state of an order.</summary>
public partial class OrderStatus : RequiredEnum<OrderStatus>
{
    /// <summary>Order is being drafted; line items can be added or removed.</summary>
    public static readonly OrderStatus Draft = new();

    /// <summary>Order has been submitted and stock reserved.</summary>
    public static readonly OrderStatus Submitted = new();

    /// <summary>Order has been approved by a warehouse manager.</summary>
    public static readonly OrderStatus Approved = new();

    /// <summary>Order has been shipped.</summary>
    public static readonly OrderStatus Shipped = new();

    /// <summary>Order has been delivered.</summary>
    public static readonly OrderStatus Delivered = new();

    /// <summary>Order has been cancelled.</summary>
    public static readonly OrderStatus Cancelled = new();
}
