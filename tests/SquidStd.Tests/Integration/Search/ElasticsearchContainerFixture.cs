using System.Net;
using DotNet.Testcontainers.Builders;
using Testcontainers.Elasticsearch;

namespace SquidStd.Tests.Integration.Search;

/// <summary>
/// Starts a single-node Elasticsearch container once for the collection (security disabled → plain HTTP) and
/// exposes its URI. The default Elasticsearch wait strategy probes over https; we override it to poll http so
/// startup completes against the security-disabled node.
/// </summary>
public sealed class ElasticsearchContainerFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _container = new ElasticsearchBuilder()
        .WithEnvironment("xpack.security.enabled", "false")
        .WithWaitStrategy(
            Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request =>
                    request.ForPort(9200).ForPath("/_cluster/health").ForStatusCode(HttpStatusCode.OK)))
        .Build();

    // Security is disabled, so the node serves plain HTTP with no auth. GetConnectionString() still returns
    // an https:// URL with credentials for 8.x images, so build the endpoint explicitly instead.
    public string ConnectionString => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(9200)}";

    public Task InitializeAsync()
        => _container.StartAsync();

    public Task DisposeAsync()
        => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(ElasticsearchCollection.Name)]
public sealed class ElasticsearchCollection : ICollectionFixture<ElasticsearchContainerFixture>
{
    public const string Name = "Elasticsearch";
}
