namespace OrderManagement.Domain;

/// <summary>
/// Order lifecycle status.
/// </summary>
public partial class OrderStatus : RequiredEnum<OrderStatus>
{
    /// <summary>Draft order.</summary>
    public static readonly OrderStatus Draft = new();

    /// <summary>Submitted order.</summary>
    public static readonly OrderStatus Submitted = new();

    /// <summary>Approved order.</summary>
    public static readonly OrderStatus Approved = new();

    /// <summary>Shipped order.</summary>
    public static readonly OrderStatus Shipped = new();

    /// <summary>Delivered order.</summary>
    public static readonly OrderStatus Delivered = new();

    /// <summary>Cancelled order.</summary>
    public static readonly OrderStatus Cancelled = new();
}
