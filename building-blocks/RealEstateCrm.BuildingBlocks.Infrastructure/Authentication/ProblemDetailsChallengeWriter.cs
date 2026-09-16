using Microsoft.AspNetCore.Http;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authentication;

/// <summary>
/// Escribe una respuesta <see cref="ProblemDetailsV1"/> estable para fallas de autenticación
/// y autorización, en vez de dejar el 401/403 vacío por defecto de ASP.NET Core.
/// </summary>
internal static class ProblemDetailsChallengeWriter
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public static async Task WriteAsync(HttpContext httpContext, int statusCode, string errorCode, string title, string? detail)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetailsV1(
            Type: "about:blank",
            Title: title,
            Status: statusCode,
            Detail: detail,
            Instance: httpContext.Request.Path,
            ErrorCode: errorCode,
            CorrelationId: ResolveCorrelationId(httpContext));

        await httpContext.Response.WriteAsJsonAsync(problem, RealEstateCrmJsonDefaults.Options);
    }

    private static Guid ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var values) &&
            Guid.TryParse(values.ToString(), out var correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid();
    }
}
