namespace OrderManagement.Application.Abstractions;

/// <summary>All permission strings defined by the system.</summary>
public static class Permissions
{
    public const string CustomersCreate = "customers:create";
    public const string ProductsCreate = "products:create";
    public const string ProductsManageStock = "products:manage-stock";
    public const string OrdersCreate = "orders:create";
    public const string OrdersSubmit = "orders:submit";
    public const string OrdersApprove = "orders:approve";
    public const string OrdersShip = "orders:ship";
    public const string OrdersDeliver = "orders:deliver";
    public const string OrdersCancel = "orders:cancel";
    public const string OrdersRead = "orders:read";
    public const string OrdersReadAll = "orders:read-all";

    public static readonly IReadOnlyList<string> All =
    [
        CustomersCreate, ProductsCreate, ProductsManageStock, OrdersCreate, OrdersSubmit,
        OrdersApprove, OrdersShip, OrdersDeliver, OrdersCancel, OrdersRead, OrdersReadAll
    ];
}

/// <summary>
/// Predefined role permission sets. Roles are not enforced by the system; they exist so that
/// callers/fixtures can construct actors with the right permission sets.
/// </summary>
public static class Roles
{
    public static readonly IReadOnlyList<string> SalesRep =
    [
        Permissions.CustomersCreate, Permissions.OrdersCreate, Permissions.OrdersSubmit,
        Permissions.OrdersCancel, Permissions.OrdersRead
    ];

    public static readonly IReadOnlyList<string> WarehouseManager =
    [
        Permissions.ProductsCreate, Permissions.ProductsManageStock, Permissions.OrdersApprove,
        Permissions.OrdersShip, Permissions.OrdersDeliver, Permissions.OrdersReadAll
    ];

    public static readonly IReadOnlyList<string> Admin = Permissions.All;
}
