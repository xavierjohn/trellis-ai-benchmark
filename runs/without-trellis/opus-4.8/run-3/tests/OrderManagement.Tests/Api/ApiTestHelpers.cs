using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;

namespace OrderManagement.Tests.Api;

public static class ApiTestHelpers
{
    public const string Version = "2026-11-12";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static HttpClient ClientFor(this OrderManagementApiFactory factory, Actor? actor = null)
    {
        var client = factory.CreateClient();
        if (actor is not null)
        {
            var payload = JsonSerializer.Serialize(new { id = actor.Id, permissions = actor.Permissions.ToArray() });
            client.DefaultRequestHeaders.Add("X-Test-Actor", payload);
        }
        return client;
    }

    public static string V(string path)
        => path.Contains('?') ? $"{path}&api-version={Version}" : $"{path}?api-version={Version}";

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<T>(Json))!;

    public static Actor Admin => OrderManagement.Application.Abstractions.Actor.Admin();
    public static Actor Sales(string id = "sales-1") => new(id,
        [Permissions.CustomersCreate, Permissions.OrdersCreate, Permissions.OrdersSubmit, Permissions.OrdersCancel, Permissions.OrdersRead]);
    public static Actor Warehouse(string id = "wh-1") => new(id,
        [Permissions.ProductsCreate, Permissions.ProductsManageStock, Permissions.OrdersApprove, Permissions.OrdersShip, Permissions.OrdersDeliver, Permissions.OrdersReadAll]);

    public static CreateCustomerRequest CustomerRequest(string email) => new(
        "Jane", "Doe", email, "+1 555 222 3333",
        new ShippingAddressDto("1 Main", "Town", "CA", "90001", "US"));

    public static CreateProductRequest ProductRequest(string sku, decimal price = 9.99m, int stock = 0) =>
        new("Widget", sku, price, stock);
}
