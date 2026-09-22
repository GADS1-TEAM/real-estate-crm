using Microsoft.AspNetCore.Mvc;
using RealEstateCrm.Contracts.Errors;

namespace PlatformConfigService.Api.ErrorHandling;

/// <summary>
/// Construye la respuesta 403 (acción prohibida) que el controller devuelve al traducir una
/// <see cref="RealEstateCrm.Contracts.Authorization.AuthorizationDecision"/> denegada
/// (aspnetcore-rest-layer skill; mismo helper que <c>AccessService.Api</c>, V2-ACL-001).
/// </summary>
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
