using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.HealthChecks;

/// <summary>
/// <see cref="RabbitMqHealthCheck"/> contra un RabbitMQ real: <c>/health/ready</c> está en
/// verde con el RabbitMQ del Compose de V2-FND-003 arriba y en rojo contra un host que no
/// resuelve/responde.
/// </summary>
/// <remarks>
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresRabbitMq"</c>. Para
/// correrlo: levantar <c>docker compose up -d rabbitmq</c> (o <c>scripts/test-integration.sh</c>).
/// </remarks>
[Trait("Category", "RequiresRabbitMq")]
public class RabbitMqReadinessCheckTests
{
    private static readonly string RabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";

    private static async Task<IHost> CreateHostAsync(string hostName)
    {
        var hostBuilder = new HostBuilder().ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton(new RabbitMqConnectionProvider(Options.Create(new RabbitMqOptions { HostName = hostName })));
                    services.AddCrmHealthChecks().AddRabbitMqReadinessCheck();
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
    public async Task Ready_is_healthy_against_a_real_rabbitmq()
    {
        using var host = await CreateHostAsync(RabbitMqHost);
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_is_unhealthy_when_rabbitmq_is_unreachable()
    {
        using var host = await CreateHostAsync("rabbitmq-host-that-does-not-exist.invalid");
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
