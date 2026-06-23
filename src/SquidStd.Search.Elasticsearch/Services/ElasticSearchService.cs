using System.Text;
using System.Text.Json.Nodes;
using Elastic.Transport;
using Serilog;
using SquidStd.Search.Abstractions.Exceptions;
using SquidStd.Search.Abstractions.Interfaces;
using SquidStd.Search.Abstractions.Search;
using SquidStd.Search.Elasticsearch.Data.Config;
using SquidStd.Search.Elasticsearch.Linq;
using HttpMethod = Elastic.Transport.HttpMethod;

namespace SquidStd.Search.Elasticsearch.Services;

/// <summary>
/// Default <see cref="ISearchService" />: indexes, deletes, and queries documents over the Elasticsearch
/// low-level transport, with index names resolved from <c>[SearchIndex]</c> + the configured prefix.
/// </summary>
public sealed class ElasticSearchService : ISearchService
{
    private readonly ILogger _logger = Log.ForContext<ElasticSearchService>();
    private readonly ElasticTransport _transport;
    private readonly string? _indexPrefix;

    public ElasticSearchService(ElasticTransport transport, ElasticsearchOptions options)
    {
        _transport = transport;
        _indexPrefix = options.IndexPrefix;
    }

    /// <summary>Resolves the (prefixed, lowercased) index name for a type.</summary>
    public string ResolveIndex<T>()
    {
        var name = SearchIndexNameResolver.Resolve(typeof(T));

        return string.IsNullOrWhiteSpace(_indexPrefix) ? name : $"{_indexPrefix}{name}".ToLowerInvariant();
    }

    /// <inheritdoc />
    public async Task IndexAsync<T>(T entity, bool refresh = false, CancellationToken cancellationToken = default)
        where T : IIndexableEntity
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.IndexId);

        var index = ResolveIndex<T>();
        var path = $"/{index}/_doc/{Uri.EscapeDataString(entity.IndexId)}{RefreshQuery(refresh)}";
        var (status, body) = await _transport.SendAsync(HttpMethod.PUT, path, ElasticTransport.SerializeDocument(entity), cancellationToken);

        EnsureSuccess(status, body, $"index document '{entity.IndexId}' into '{index}'");
    }

    /// <inheritdoc />
    public async Task IndexManyAsync<T>(IEnumerable<T> entities, bool refresh = false, CancellationToken cancellationToken = default)
        where T : IIndexableEntity
    {
        ArgumentNullException.ThrowIfNull(entities);

        var index = ResolveIndex<T>();
        var lines = new StringBuilder();

        foreach (var entity in entities)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entity.IndexId);
            var action = new JsonObject { ["index"] = new JsonObject { ["_id"] = entity.IndexId } };
            lines.Append(action.ToJsonString()).Append('\n');
            lines.Append(ElasticTransport.SerializeDocument(entity).ToJsonString()).Append('\n');
        }

        if (lines.Length == 0)
        {
            return;
        }

        var path = $"/{index}/_bulk{RefreshQuery(refresh)}";
        var (status, body) = await _transport.SendRawAsync(HttpMethod.POST, path, lines.ToString(), cancellationToken);

        EnsureSuccess(status, body, $"bulk index into '{index}'");

        if (body?["errors"]?.GetValue<bool>() == true)
        {
            throw new SearchException($"Bulk index into '{index}' reported item errors.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync<T>(string indexId, bool refresh = false, CancellationToken cancellationToken = default)
        where T : IIndexableEntity
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexId);

        var index = ResolveIndex<T>();
        var path = $"/{index}/_doc/{Uri.EscapeDataString(indexId)}{RefreshQuery(refresh)}";
        var (status, body) = await _transport.SendAsync(HttpMethod.DELETE, path, null, cancellationToken);

        if (status == 404)
        {
            return false;
        }

        EnsureSuccess(status, body, $"delete document '{indexId}' from '{index}'");

        return body?["result"]?.GetValue<string>() == "deleted";
    }

    /// <inheritdoc />
    public async Task EnsureIndexAsync<T>(CancellationToken cancellationToken = default)
        where T : IIndexableEntity
    {
        var index = ResolveIndex<T>();
        var (existsStatus, _) = await _transport.SendAsync(HttpMethod.HEAD, $"/{index}", null, cancellationToken);

        if (existsStatus == 200)
        {
            return;
        }

        var (status, body) = await _transport.SendAsync(HttpMethod.PUT, $"/{index}", null, cancellationToken);

        if (status == 400 && body?["error"]?["type"]?.GetValue<string>() == "resource_already_exists_exception")
        {
            return;
        }

        EnsureSuccess(status, body, $"create index '{index}'");
    }

    /// <inheritdoc />
    public IQueryable<T> Query<T>() where T : IIndexableEntity
        => new ElasticQueryable<T>(new ElasticQueryProvider(_transport, ResolveIndex<T>(), typeof(T)));

    private static string RefreshQuery(bool refresh)
        => refresh ? "?refresh=wait_for" : string.Empty;

    private void EnsureSuccess(int status, JsonNode? body, string operation)
    {
        if (status is >= 200 and < 300)
        {
            return;
        }

        var reason = body?["error"]?["reason"]?.GetValue<string>() ?? $"HTTP {status}";
        _logger.Error("Elasticsearch failed to {Operation}: {Reason}", operation, reason);

        throw new SearchException($"Elasticsearch failed to {operation}: {reason}");
    }
}
