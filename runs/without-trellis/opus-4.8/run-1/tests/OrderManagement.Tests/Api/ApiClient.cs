using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderManagement.Tests.Api;

/// <summary>Helpers for issuing versioned, actor-scoped HTTP requests in integration tests.</summary>
internal static class ApiClient
{
    public const string Version = "2026-11-12";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string WithVersion(string path)
        => path.Contains('?') ? $"{path}&api-version={Version}" : $"{path}?api-version={Version}";

    public static HttpRequestMessage Request(HttpMethod method, string path, string? actorJson = null, object? body = null)
    {
        var request = new HttpRequestMessage(method, WithVersion(path));
        if (actorJson is not null)
            request.Headers.Add("X-Test-Actor", actorJson);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    public static string Actor(string id, params string[] permissions)
        => JsonSerializer.Serialize(new { id, permissions });

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<T>(Json))!;
}
