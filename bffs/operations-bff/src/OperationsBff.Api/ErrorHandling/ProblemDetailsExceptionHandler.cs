using Microsoft.AspNetCore.Diagnostics;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Serialization;

namespace OperationsBff.Api.ErrorHandling;

/// <summary>
/// Manejador centralizado de excepciones del BFF (skill aspnetcore-error-and-observability). Las
/// respuestas de error que ya vienen de access-service las reenvía <c>ProxyResults</c> tal cual
/// (con su propio <c>ProblemDetailsV1</c>); esto solo cubre fallas propias del BFF (ej. request
/// malformado antes de llegar a un controller, access-service inalcanzable).
/// </summary>
public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error no manejado en operations-bff.");

        var correlationId = httpContext.Response.Headers.TryGetValue("X-Correlation-Id", out var headerValue)
            && Guid.TryParse(headerValue.ToString(), out var parsedCorrelationId)
                ? parsedCorrelationId
                : Guid.NewGuid();

        var problem = new ProblemDetailsV1(
            Type: "about:blank",
            Title: "Ocurrió un error inesperado.",
            Status: StatusCodes.Status502BadGateway,
            Detail: null,
            Instance: httpContext.Request.Path,
            ErrorCode: "bad_gateway",
            CorrelationId: correlationId);

        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, RealEstateCrmJsonDefaults.Options, cancellationToken);

        return true;
    }
}
