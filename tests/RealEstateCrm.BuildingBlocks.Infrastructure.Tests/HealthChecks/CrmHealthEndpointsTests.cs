using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.HealthChecks;

/// <summary>
/// OPS-003: <c>/health/live</c> no evalúa dependencias (el proceso solo confirma que está
/// vivo) y <c>/health/ready</c> sí, así que uno puede estar en verde mientras el otro está en
/// rojo. No necesita Mongo/RabbitMQ reales: usa <see cref="IHealthCheck"/> fakes registrados
/// con el tag "ready" para simular una dependencia caída.
/// </summary>
public class CrmHealthEndpointsTests
{
    private sealed class AlwaysUnhealthyCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(HealthCheckResult.Unhealthy("dependencia caída (fake)"));
    }

    private sealed class AlwaysHealthyCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(HealthCheckResult.Healthy());
    }

    private static async Task<IHost> CreateHostAsync(Action<IHealthChecksBuilder> configureChecks)
    {
        var hostBuilder = new HostBuilder().ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    configureChecks(services.AddCrmHealthChecks());
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapCrmHealthEndpoints());
                });
        });

        return await hostBuilder.StartAsync();
    }

    [Fact]
    public async Task Live_is_healthy_even_when_a_ready_dependency_is_down()
    {
        using var host = await CreateHostAsync(checks => checks.AddCheck<AlwaysUnhealthyCheck>("fake-dependency", tags: [CrmHealthCheckTags.Ready]));
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_is_unhealthy_when_a_ready_dependency_is_down()
    {
        using var host = await CreateHostAsync(checks => checks.AddCheck<AlwaysUnhealthyCheck>("fake-dependency", tags: [CrmHealthCheckTags.Ready]));
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Ready_is_healthy_when_all_ready_dependencies_are_up()
    {
        using var host = await CreateHostAsync(checks => checks.AddCheck<AlwaysHealthyCheck>("fake-dependency", tags: [CrmHealthCheckTags.Ready]));
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_ignores_checks_without_the_ready_tag()
    {
        using var host = await CreateHostAsync(checks => checks.AddCheck<AlwaysUnhealthyCheck>("untagged-check"));
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
