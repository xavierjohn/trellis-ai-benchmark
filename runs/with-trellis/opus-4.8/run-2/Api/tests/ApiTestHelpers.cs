namespace Api.Tests;

using System.Net.Http.Json;

/// <summary>Shared helpers for building versioned URLs and JSON request bodies.</summary>
internal static class ApiTestHelpers
{
    public const string Version = "2026-11-12";

    public static string Url(string path) =>
        path.Contains('?') ? $"{path}&api-version={Version}" : $"{path}?api-version={Version}";

    public static object CustomerBody(string email, string? phone = null) => new
    {
        firstName = "Jane",
        lastName = "Doe",
        email,
        phoneNumber = phone,
        shippingAddress = new
        {
            street = "1 Main St",
            city = "Springfield",
            state = "IL",
            postalCode = "62701",
            country = "USA",
        },
    };

    public static object ProductBody(string sku, decimal price = 9.99m) => new
    {
        name = "Widget",
        sku,
        unitPrice = price,
    };

    public static Task<HttpResponseMessage> PostJson(this HttpClient client, string path, object body, CancellationToken ct) =>
        client.PostAsJsonAsync(Url(path), body, ct);

    public static Task<HttpResponseMessage> PostEmpty(this HttpClient client, string path, CancellationToken ct) =>
        client.PostAsync(Url(path), content: null, ct);

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public static string UniqueSku() => $"SKU{Guid.NewGuid():N}"[..18].ToUpperInvariant();
}
