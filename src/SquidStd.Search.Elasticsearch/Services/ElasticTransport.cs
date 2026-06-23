using System.Text.Json;
using System.Text.Json.Nodes;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using HttpMethod = Elastic.Transport.HttpMethod;

namespace SquidStd.Search.Elasticsearch.Services;

/// <summary>
/// Thin JSON request helper over the Elasticsearch client's low-level transport. Sends raw DSL/document JSON
/// and returns the parsed response, decoupling the provider from the strongly-typed query DSL.
/// </summary>
public sealed class ElasticTransport
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    // Elasticsearch 8.x rejects requests without an explicit media type, so force it on every request.
    private static readonly RequestConfiguration JsonRequest = new()
    {
        Accept = "application/json",
        ContentType = "application/json"
    };

    // The bulk API expects newline-delimited JSON.
    private static readonly RequestConfiguration NdjsonRequest = new()
    {
        Accept = "application/json",
        ContentType = "application/x-ndjson"
    };

    private readonly ElasticsearchClient _client;

    public ElasticTransport(ElasticsearchClient client)
    {
        _client = client;
    }

    /// <summary>Sends a request with an optional JSON body and returns (statusCode, bodyJson).</summary>
    public Task<(int Status, JsonNode? Body)> SendAsync(
        HttpMethod method,
        string path,
        JsonNode? body,
        CancellationToken cancellationToken)
        => SendCoreAsync(method, path, body?.ToJsonString(), JsonRequest, cancellationToken);

    /// <summary>Sends a raw (already-serialized) NDJSON body for the bulk API.</summary>
    public Task<(int Status, JsonNode? Body)> SendRawAsync(
        HttpMethod method,
        string path,
        string? body,
        CancellationToken cancellationToken)
        => SendCoreAsync(method, path, body, NdjsonRequest, cancellationToken);

    private async Task<(int Status, JsonNode? Body)> SendCoreAsync(
        HttpMethod method,
        string path,
        string? body,
        RequestConfiguration requestConfiguration,
        CancellationToken cancellationToken)
    {
        var postData = body is null ? null : PostData.String(body);

        var response = await _client.Transport
            .RequestAsync<StringResponse>(method, path, postData, requestConfiguration, cancellationToken)
            .ConfigureAwait(false);

        var status = response.ApiCallDetails.HttpStatusCode ?? 0;
        var text = response.Body;
        var node = string.IsNullOrEmpty(text) ? null : JsonNode.Parse(text);

        return (status, node);
    }

    /// <summary>Serializes a value to a <see cref="JsonNode" /> using Web (camelCase) defaults.</summary>
    public static JsonNode SerializeDocument<T>(T value)
        => JsonSerializer.SerializeToNode(value, WebOptions)!;

    /// <summary>Deserializes a document body (an Elasticsearch <c>_source</c>) to <typeparamref name="T" />.</summary>
    public static T DeserializeDocument<T>(JsonNode source)
        => source.Deserialize<T>(WebOptions)!;
}
