namespace OrderManagement.Domain;

/// <summary>Permission constants for order management operations.</summary>
public static class Permissions
{
    /// <summary>Create new customers.</summary>
    public const string CustomersCreate = "customers:create";

    /// <summary>Create new products.</summary>
    public const string ProductsCreate = "products:create";

    /// <summary>Add stock to products.</summary>
    public const string ProductsManageStock = "products:manage-stock";

    /// <summary>Create draft orders and manage line items.</summary>
    public const string OrdersCreate = "orders:create";

    /// <summary>Submit draft orders.</summary>
    public const string OrdersSubmit = "orders:submit";

    /// <summary>Approve submitted orders.</summary>
    public const string OrdersApprove = "orders:approve";

    /// <summary>Ship approved orders.</summary>
    public const string OrdersShip = "orders:ship";

    /// <summary>Mark shipped orders as delivered.</summary>
    public const string OrdersDeliver = "orders:deliver";

    /// <summary>Cancel orders (subject to ownership check).</summary>
    public const string OrdersCancel = "orders:cancel";

    /// <summary>View orders.</summary>
    public const string OrdersRead = "orders:read";

    /// <summary>View any customer's orders and overdue orders.</summary>
    public const string OrdersReadAll = "orders:read-all";

    /// <summary>Every permission the system defines (the Admin role).</summary>
    public static readonly IReadOnlyList<string> All =
    [
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
    ];
}
