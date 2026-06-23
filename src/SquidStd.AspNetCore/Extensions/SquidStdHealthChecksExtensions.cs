using DryIoc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using SquidStd.AspNetCore.Services;
using SquidHealthCheck = SquidStd.Core.Interfaces.Health.IHealthCheck;

namespace SquidStd.AspNetCore.Extensions;

/// <summary>
/// Extension methods that bridge SquidStd health checks into the standard ASP.NET Core health-check system.
/// </summary>
public static class SquidStdHealthChecksExtensions
{
    /// <param name="builder">ASP.NET Core application builder.</param>
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Registers each SquidStd <c>IHealthCheck</c> as a standard ASP.NET Core health check (one entry
        /// per check, same name). Call after <c>UseSquidStd</c>; expose them with the standard
        /// <c>app.MapHealthChecks(...)</c>. Check names must be unique.
        /// </summary>
        /// <returns>The same builder for chaining.</returns>
        public WebApplicationBuilder AddSquidStdHealthChecks()
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (!builder.Host.Properties.TryGetValue(SquidStdAspNetCoreBuilderExtensions.ContainerPropertyKey, out var value)
                || value is not IContainer container)
            {
                throw new InvalidOperationException("Call UseSquidStd before AddSquidStdHealthChecks.");
            }

            var checks = container.Resolve<IEnumerable<SquidHealthCheck>>();
            var healthChecks = builder.Services.AddHealthChecks();

            foreach (var check in checks)
            {
                healthChecks.Add(
                    new HealthCheckRegistration(
                        check.Name,
                        _ => new SquidStdHealthCheckAdapter(check),
                        failureStatus: HealthStatus.Unhealthy,
                        tags: null
                    )
                );
            }

            return builder;
        }
    }
}
