namespace OrderManagement.Domain;

/// <summary>
/// Permission names used by the order management service.
/// </summary>
public static class Permissions
{
    /// <summary>Create customers.</summary>
    public const string CustomersCreate = "customers:create";

    /// <summary>Create products.</summary>
    public const string ProductsCreate = "products:create";

    /// <summary>Manage product stock.</summary>
    public const string ProductsManageStock = "products:manage-stock";

    /// <summary>Create orders and manage draft line items.</summary>
    public const string OrdersCreate = "orders:create";

    /// <summary>Submit draft orders.</summary>
    public const string OrdersSubmit = "orders:submit";

    /// <summary>Approve submitted orders.</summary>
    public const string OrdersApprove = "orders:approve";

    /// <summary>Ship approved orders.</summary>
    public const string OrdersShip = "orders:ship";

    /// <summary>Mark shipped orders as delivered.</summary>
    public const string OrdersDeliver = "orders:deliver";

    /// <summary>Cancel orders.</summary>
    public const string OrdersCancel = "orders:cancel";

    /// <summary>Read orders.</summary>
    public const string OrdersRead = "orders:read";

    /// <summary>Read all orders and customer order lists.</summary>
    public const string OrdersReadAll = "orders:read-all";

    /// <summary>All permissions.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        CustomersCreate,
        ProductsCreate,
        ProductsManageStock,
        OrdersCreate,
        OrdersSubmit,
        OrdersApprove,
        OrdersShip,
        OrdersDeliver,
        OrdersCancel,
        OrdersRead,
        OrdersReadAll,
    };
}
