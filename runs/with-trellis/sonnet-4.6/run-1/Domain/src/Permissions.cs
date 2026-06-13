namespace OrderManagement.Domain;

/// <summary>
/// Permission constants for the Order Management system.
/// </summary>
public static class Permissions
{
    /// <summary>Allows creating customers.</summary>
    public const string CustomersCreate = "customers:create";

    /// <summary>Allows creating products.</summary>
    public const string ProductsCreate = "products:create";

    /// <summary>Allows managing product stock.</summary>
    public const string ProductsManageStock = "products:manage-stock";

    /// <summary>Allows creating draft orders and editing draft line items.</summary>
    public const string OrdersCreate = "orders:create";

    /// <summary>Allows submitting orders.</summary>
    public const string OrdersSubmit = "orders:submit";

    /// <summary>Allows approving submitted orders.</summary>
    public const string OrdersApprove = "orders:approve";

    /// <summary>Allows shipping approved orders.</summary>
    public const string OrdersShip = "orders:ship";

    /// <summary>Allows delivering shipped orders.</summary>
    public const string OrdersDeliver = "orders:deliver";

    /// <summary>Allows cancelling orders.</summary>
    public const string OrdersCancel = "orders:cancel";

    /// <summary>Allows reading orders owned by the actor.</summary>
    public const string OrdersRead = "orders:read";

    /// <summary>Allows reading all orders.</summary>
    public const string OrdersReadAll = "orders:read-all";
}
