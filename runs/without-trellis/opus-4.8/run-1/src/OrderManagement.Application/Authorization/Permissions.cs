namespace OrderManagement.Application.Authorization;

/// <summary>Canonical permission names used across the system.</summary>
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

    public static readonly IReadOnlyList<string> All = new[]
    {
        CustomersCreate, ProductsCreate, ProductsManageStock, OrdersCreate, OrdersSubmit,
        OrdersApprove, OrdersShip, OrdersDeliver, OrdersCancel, OrdersRead, OrdersReadAll
    };
}
