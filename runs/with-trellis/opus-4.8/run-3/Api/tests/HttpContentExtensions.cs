namespace Api.Tests;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

public static class HttpContentExtensions
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> that sends an <c>X-Test-Actor</c> header identifying
    /// the supplied actor id and permission set, as understood by the development actor provider.
    /// </summary>
    public static HttpClient CreateClientWithActor<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory, string actorId, params string[] permissions)
        where TEntryPoint : class
    {
        var client = factory.CreateClient();
        var payload = JsonSerializer.Serialize(new { Id = actorId, Permissions = permissions });
        client.DefaultRequestHeaders.Add("X-Test-Actor", payload);
        return client;
    }

    static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static async Task<T> ReadAsAsyncWithAssertion<T>(this HttpContent content)
    {
        var str = await content.ReadAsStringAsync();
        str.Should().NotBeNull();

        var t = JsonSerializer.Deserialize<T>(str, s_options);
        if (t == null)
            throw new InvalidOperationException("Failed to deserialize");

        return t;
    }

    public static Task<T> ReadAsExample<T>(this HttpContent content, T example)
        => content.ReadAsAsyncWithAssertion<T>();

    /// <summary>
    /// Issues a JSON POST with a freshly-generated <c>Idempotency-Key</c> header so the request
    /// satisfies the <c>[Idempotent]</c> opt-in middleware on the Todos create endpoint.
    /// Each call uses a unique key, so retries are not replayed.
    /// </summary>
    public static Task<HttpResponseMessage> PostJsonIdempotentAsync<T>(
        this HttpClient client, string url, T body, CancellationToken cancellationToken)
        => client.PostJsonWithKeyAsync(url, body, Guid.NewGuid().ToString(), cancellationToken);

    /// <summary>
    /// Issues a JSON POST with a caller-supplied <c>Idempotency-Key</c> header.
    /// Used by replay tests that intentionally retry with the same key to exercise the
    /// middleware's snapshot/replay path.
    /// </summary>
    public static Task<HttpResponseMessage> PostJsonWithKeyAsync<T>(
        this HttpClient client, string url, T body, string idempotencyKey, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request, cancellationToken);
    }
}
