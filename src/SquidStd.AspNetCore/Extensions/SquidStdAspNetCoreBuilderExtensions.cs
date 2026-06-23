using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SquidStd.AspNetCore.Services;
using SquidStd.Core.Data.Bootstrap;
using SquidStd.Services.Core.Services.Bootstrap;

namespace SquidStd.AspNetCore.Extensions;

/// <summary>
/// Extension methods for connecting SquidStd to ASP.NET Core Minimal API applications.
/// </summary>
public static class SquidStdAspNetCoreBuilderExtensions
{
    /// <param name="builder">ASP.NET Core application builder.</param>
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Registers SquidStd using DryIoc as the ASP.NET Core service provider.
        /// </summary>
        /// <param name="configureOptions">Optional SquidStd bootstrap options callback.</param>
        /// <returns>The same builder for chaining.</returns>
        public WebApplicationBuilder UseSquidStd(Action<SquidStdOptions>? configureOptions = null)
            => builder.UseSquidStd(configureOptions, null);

        /// <summary>
        /// Registers SquidStd using DryIoc as the ASP.NET Core service provider.
        /// </summary>
        /// <param name="configureOptions">Optional SquidStd bootstrap options callback.</param>
        /// <param name="configureContainer">Optional DryIoc registration callback.</param>
        /// <returns>The same builder for chaining.</returns>
        public WebApplicationBuilder UseSquidStd(
            Action<SquidStdOptions>? configureOptions,
            Func<IContainer, IContainer>? configureContainer
        )
        {
            ArgumentNullException.ThrowIfNull(builder);

            var options = new SquidStdOptions
            {
                RootDirectory = builder.Environment.ContentRootPath
            };
            configureOptions?.Invoke(options);
            ValidateOptions(options);

            var container = new Container();
            builder.Host.UseServiceProviderFactory(new DryIocServiceProviderFactory(container));
            SquidStdBootstrap.Create(options, container);
            builder.Host.Properties[ContainerPropertyKey] = container;

            var configuredContainer = configureContainer?.Invoke(container) ?? container;

            if (!ReferenceEquals(configuredContainer, container))
            {
                throw new InvalidOperationException("ConfigureSquidStdContainer must return the DryIoc container instance.");
            }

            builder.Services.AddHostedService<SquidStdHostedService>();

            return builder;
        }
    }

    internal const string ContainerPropertyKey = "SquidStd:Container";

    private static void ValidateOptions(SquidStdOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConfigName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.RootDirectory);
    }
}
