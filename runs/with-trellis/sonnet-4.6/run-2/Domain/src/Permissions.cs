namespace OrderManagement.Domain;

/// <summary>
/// Permission constants for all operations.
/// </summary>
public static class Permissions
{
    public const string TodosCreate = "todos:create";
    public const string TodosRead = "todos:read";
    public const string TodosUpdate = "todos:update";
    public const string TodosComplete = "todos:complete";
    public const string TodosDelete = "todos:delete";

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
}
