using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.HealthChecks;

/// <summary>
/// <see cref="MongoHealthCheck"/> contra un Mongo real: <c>/health/ready</c> está en verde con
/// el Mongo del Compose de V2-FND-003 arriba y en rojo contra un puerto sin nada escuchando.
/// </summary>
/// <remarks>
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresMongo"</c>. Para correrlo:
/// levantar <c>docker compose up -d mongo</c> (o <c>scripts/test-integration.sh</c>).
/// </remarks>
[Trait("Category", "RequiresMongo")]
public class MongoReadinessCheckTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017/?directConnection=true";

    private static async Task<IHost> CreateHostAsync(string connectionString)
    {
        var hostBuilder = new HostBuilder().ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
                    services.AddCrmHealthChecks().AddMongoReadinessCheck();
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
    public async Task Ready_is_healthy_against_a_real_mongo()
    {
        using var host = await CreateHostAsync(ConnectionString);
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_is_unhealthy_when_mongo_is_unreachable()
    {
        // Puerto sin nada escuchando + timeout de selección de servidor corto para no colgar el test.
        using var host = await CreateHostAsync("mongodb://localhost:27099/?connectTimeoutMS=500&serverSelectionTimeoutMS=1000");
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
