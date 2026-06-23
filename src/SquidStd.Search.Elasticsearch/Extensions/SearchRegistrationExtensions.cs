using DryIoc;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using SquidStd.Search.Abstractions.Interfaces;
using SquidStd.Search.Elasticsearch.Data.Config;
using SquidStd.Search.Elasticsearch.Services;

namespace SquidStd.Search.Elasticsearch.Extensions;

/// <summary>DryIoc registration helpers for the Elasticsearch search provider.</summary>
public static class SearchRegistrationExtensions
{
    /// <summary>Registers the Elasticsearch client, transport helper, and <see cref="ISearchService" />.</summary>
    public static IContainer AddElasticsearch(this IContainer container, ElasticsearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(options);

        container.RegisterInstance(options);
        container.RegisterDelegate(_ => CreateClient(options), Reuse.Singleton);
        container.Register<ElasticTransport>(Reuse.Singleton);
        container.Register<ISearchService, ElasticSearchService>(Reuse.Singleton);

        return container;
    }

    private static ElasticsearchClient CreateClient(ElasticsearchOptions options)
    {
        var settings = new ElasticsearchClientSettings(options.Uri);

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            settings = settings.Authentication(new ApiKey(options.ApiKey));
        }
        else if (!string.IsNullOrWhiteSpace(options.Username))
        {
            settings = settings.Authentication(new BasicAuthentication(options.Username, options.Password ?? string.Empty));
        }

        if (options.AllowUntrustedCertificate)
        {
            settings = settings.ServerCertificateValidationCallback((_, _, _, _) => true);
        }

        return new ElasticsearchClient(settings);
    }
}
