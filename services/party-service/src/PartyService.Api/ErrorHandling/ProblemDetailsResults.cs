using Microsoft.AspNetCore.Mvc;
using RealEstateCrm.Contracts.Errors;

namespace PartyService.Api.ErrorHandling;

/// <summary>Construye el 403 (acción prohibida, D4) al traducir una decisión de autorización denegada.</summary>
internal static class ProblemDetailsResults
{
    public static ObjectResult Forbidden(string reasonCode, Guid correlationId, string instance) =>
        new(new ProblemDetailsV1(
            Type: "about:blank",
            Title: "Acción prohibida.",
            Status: StatusCodes.Status403Forbidden,
            Detail: reasonCode,
            Instance: instance,
            ErrorCode: ErrorCodes.Forbidden,
            CorrelationId: correlationId))
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };

    public static ObjectResult Unauthorized(string detail, Guid correlationId, string instance) =>
        new(new ProblemDetailsV1(
            Type: "about:blank",
            Title: "No autenticado.",
            Status: StatusCodes.Status401Unauthorized,
            Detail: detail,
            Instance: instance,
            ErrorCode: ErrorCodes.Unauthorized,
            CorrelationId: correlationId))
        {
            StatusCode = StatusCodes.Status401Unauthorized,
        };
}
