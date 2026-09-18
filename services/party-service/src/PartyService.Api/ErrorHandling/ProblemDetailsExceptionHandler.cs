using Microsoft.AspNetCore.Diagnostics;
using PartyService.Application;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Serialization;

namespace PartyService.Api.ErrorHandling;

/// <summary>
/// Manejador centralizado de excepciones: ningún error queda tragado en silencio ni devuelve un
/// stack trace al cliente; todo sale como <see cref="ProblemDetailsV1"/> con un <c>errorCode</c> estable.
/// </summary>
public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public const string DependencyUnavailableErrorCode = "dependency_unavailable";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, errorCode, title, detail) = exception switch
        {
            PartyDomainException domainEx => (domainEx.HttpStatus, domainEx.ErrorCode, Titles.For(domainEx.HttpStatus), domainEx.Message),
            ConcurrencyConflictException concurrencyEx => (
                StatusCodes.Status409Conflict,
                ErrorCodes.Conflict,
                "Conflicto de concurrencia: el recurso cambió mientras tanto.",
                concurrencyEx.Message),
            HttpRequestException => (
                StatusCodes.Status503ServiceUnavailable,
                DependencyUnavailableErrorCode,
                "Un servicio del que depende esta operación no está disponible.",
                (string?)null),
            _ => (0, string.Empty, string.Empty, (string?)null),
        };

        if (status == 0)
        {
            logger.LogError(exception, "Error no manejado en party-service.");
            status = StatusCodes.Status500InternalServerError;
            errorCode = "internal_error";
            title = "Ocurrió un error inesperado.";
            detail = null; // sin stack trace ni mensaje interno al cliente (OWASP baseline).
        }
        else if (exception is HttpRequestException)
        {
            logger.LogError(exception, "Dependencia HTTP no disponible o con error en party-service.");
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

    private static class Titles
    {
        public static string For(int status) => status switch
        {
            StatusCodes.Status403Forbidden => "Acción prohibida.",
            StatusCodes.Status404NotFound => "Recurso inexistente.",
            StatusCodes.Status409Conflict => "Conflicto con el estado actual.",
            _ => "Error de validación de negocio.",
        };
    }
}
