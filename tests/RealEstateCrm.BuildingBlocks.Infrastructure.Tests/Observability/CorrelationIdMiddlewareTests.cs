using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealEstateCrm.BuildingBlocks.Infrastructure.Observability;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Observability;

/// <summary>
/// OPS-004: <see cref="CorrelationIdMiddleware"/> genera o propaga el correlationId por
/// request, lo devuelve en la respuesta, y cualquier log emitido dentro del handler lo incluye
/// en scope sin que el handler lo pida explícitamente.
/// </summary>
public class CorrelationIdMiddlewareTests
{
    private static async Task<(IHost Host, StringWriter LogOutput)> CreateHostAsync()
    {
        var logOutput = new StringWriter();

        var hostBuilder = new HostBuilder().ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddLogging(logging => logging.AddProvider(new SanitizingLoggerProvider(logOutput)));
                })
                .Configure(app =>
                {
                    app.UseCrmCorrelationId();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/ping", (ILogger<CorrelationIdMiddlewareTests> logger) =>
                        {
                            logger.LogInformation("handling ping");
                            return Results.Ok();
                        });
                    });
                });
        });

        var host = await hostBuilder.StartAsync();
        return (host, logOutput);
    }

    [Fact]
    public async Task Generates_a_correlationId_when_the_request_has_none()
    {
        var (host, _) = await CreateHostAsync();
        using (host)
        {
            using var client = host.GetTestClient();

            var response = await client.GetAsync("/ping");

            Assert.True(response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values));
            Assert.False(string.IsNullOrWhiteSpace(values!.Single()));
        }
    }

    [Fact]
    public async Task Echoes_back_the_correlationId_sent_by_the_caller()
    {
        var (host, _) = await CreateHostAsync();
        using (host)
        {
            using var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "caller-provided-cid");

            var response = await client.GetAsync("/ping");

            var returned = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
            Assert.Equal("caller-provided-cid", returned);
        }
    }

    [Fact]
    public async Task Logs_emitted_while_handling_the_request_include_the_correlationId()
    {
        var (host, logOutput) = await CreateHostAsync();
        using (host)
        {
            using var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "cid-for-log-scope");

            await client.GetAsync("/ping");
        }

        Assert.Contains("cid-for-log-scope", logOutput.ToString());
    }
}
