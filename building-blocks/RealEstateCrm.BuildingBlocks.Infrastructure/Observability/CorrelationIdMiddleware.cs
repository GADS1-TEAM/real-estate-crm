using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Observability;

/// <summary>
/// Toma o genera un correlationId por request, lo agrega como tag de <see cref="Activity.Current"/>
/// (para que viaje en las trazas de OpenTelemetry), lo devuelve en la respuesta y abre un
/// <see cref="ILogger.BeginScope{TState}"/> con correlationId/actorId para que todo log dentro
/// del request los incluya sin que cada línea los pida explícitamente (OPS-004).
/// </summary>
/// <remarks>
/// No hay ningún <c>Program.cs</c> de <c>services/</c>/<c>bffs/</c> que registre este middleware
/// todavía (fuera de la write zone de V2-FND-003). El actorId se toma del claim <c>sub</c> del
/// usuario autenticado cuando existe; sin sesión autenticada no hay actorId.
/// </remarks>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString();

        var actorId = context.User.FindFirst("sub")?.Value;

        context.Response.Headers[HeaderName] = correlationId;

        Activity.Current?.SetTag("correlationId", correlationId);
        if (actorId is not null)
        {
            Activity.Current?.SetTag("actorId", actorId);
        }

        var scopeState = new Dictionary<string, object?> { ["correlationId"] = correlationId };
        if (actorId is not null)
        {
            scopeState["actorId"] = actorId;
        }

        using (logger.BeginScope(scopeState))
        {
            await next(context);
        }
    }
}
