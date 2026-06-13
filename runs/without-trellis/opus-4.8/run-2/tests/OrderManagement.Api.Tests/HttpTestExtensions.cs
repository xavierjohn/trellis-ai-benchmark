using System.Net.Http.Json;
using System.Text.Json;
using OrderManagement.Application.Abstractions;

namespace OrderManagement.Api.Tests;

public static class TestActors
{
    public static string Json(string id, params string[] permissions) =>
        JsonSerializer.Serialize(new { id, permissions });

    public static string Admin(string id = "admin-1") => Json(id, [.. Roles.Admin]);
    public static string SalesRep(string id) => Json(id, [.. Roles.SalesRep]);
    public static string Warehouse(string id) => Json(id, [.. Roles.WarehouseManager]);
}

public static class HttpTestExtensions
{
    public const string Version = "2026-11-12";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task<HttpResponseMessage> PostJsonAsync(
        this HttpClient client, string path, object? body, string? actor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, WithVersion(path))
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        AddActor(request, actor);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> GetAsync(this HttpClient client, string path, string? actor)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, WithVersion(path));
        AddActor(request, actor);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> DeleteAsync(this HttpClient client, string path, string? actor)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, WithVersion(path));
        AddActor(request, actor);
        return client.SendAsync(request);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions)!;
    }

    private static string WithVersion(string path) =>
        path.Contains('?') ? $"{path}&api-version={Version}" : $"{path}?api-version={Version}";

    private static void AddActor(HttpRequestMessage request, string? actor)
    {
        if (actor is not null)
            request.Headers.Add("X-Test-Actor", actor);
    }
}
