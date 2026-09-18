using Microsoft.AspNetCore.Diagnostics;
using PlatformConfigService.Application;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Serialization;

namespace PlatformConfigService.Api.ErrorHandling;

/// <summary>
/// Manejador centralizado de excepciones (skill aspnetcore-error-and-observability): ningún
/// error queda tragado en silencio ni devuelve un stack trace al cliente; todo sale como
/// <see cref="ProblemDetailsV1"/> con un <c>errorCode</c> estable.
/// </summary>
public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, errorCode, title, detail) = exception switch
        {
            PlatformConfigDomainException domainEx => (domainEx.HttpStatus, domainEx.ErrorCode, "Error de validación de negocio.", domainEx.Message),
            ConcurrencyConflictException concurrencyEx => (
                StatusCodes.Status409Conflict,
                ErrorCodes.Conflict,
                "Conflicto de concurrencia: el recurso cambió mientras tanto.",
                concurrencyEx.Message),
            _ => (0, string.Empty, string.Empty, (string?)null),
        };

        if (status == 0)
        {
            logger.LogError(exception, "Error no manejado en platform-config-service.");
            status = StatusCodes.Status500InternalServerError;
            errorCode = "internal_error";
            title = "Ocurrió un error inesperado.";
            detail = null; // sin stack trace ni mensaje interno al cliente (OWASP baseline).
        }

        var problem = new ProblemDetailsV1(
            Type: "about:blank",
            Title: title,
            Status: status,
            Detail: detail,
            Instance: httpContext.Request.Path,
            ErrorCode: errorCode,
            CorrelationId: ResolveCorrelationId(httpContext));

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, RealEstateCrmJsonDefaults.Options, cancellationToken);

        return true;
    }

    private static Guid ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Response.Headers.TryGetValue("X-Correlation-Id", out var values) &&
            Guid.TryParse(values.ToString(), out var correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid();
    }
}
