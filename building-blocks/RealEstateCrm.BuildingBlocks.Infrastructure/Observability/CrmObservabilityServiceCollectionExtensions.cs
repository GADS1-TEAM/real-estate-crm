using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Observability;

/// <summary>
/// Logs estructurados con redacción (<see cref="SanitizingLoggerProvider"/>) + trazas
/// OpenTelemetry con instrumentación de ASP.NET Core (OPS-004/OPS-005, V2-FND-003). El exporter
/// es de consola: la POC no requiere collector ni dashboard externo (overrides de la task).
/// </summary>
/// <remarks>
/// Ningún <c>Program.cs</c> de <c>services/</c>/<c>bffs/</c> invoca esto todavía (fuera de la
/// write zone de V2-FND-003).
/// </remarks>
public static class CrmObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddCrmObservability(this IServiceCollection services, string serviceName)
    {
        services.AddLogging(logging => logging.AddProvider(new SanitizingLoggerProvider(Console.Out)));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddConsoleExporter());

        return services;
    }

    /// <summary>Registra <see cref="CorrelationIdMiddleware"/> en el pipeline HTTP.</summary>
    public static IApplicationBuilder UseCrmCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
