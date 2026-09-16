using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Mapea <c>/health/live</c> (el proceso responde, sin evaluar dependencias) y
/// <c>/health/ready</c> (evalúa los checks con el tag <see cref="CrmHealthCheckTags.Ready"/>,
/// típicamente Mongo/RabbitMQ) según OPS-003.
/// </summary>
public static class CrmHealthCheckEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCrmHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(CrmHealthCheckTags.Ready),
        });

        return endpoints;
    }
}
