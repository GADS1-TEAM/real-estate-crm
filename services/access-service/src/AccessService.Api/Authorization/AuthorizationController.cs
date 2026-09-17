using AccessService.Application.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Authorization;

namespace AccessService.Api.Authorization;

/// <summary>
/// Endpoint HTTP interno de AUTHZ-002 (D2, plan Wave 2 sección 5): la matriz rol→permiso vive
/// solo acá; los demás servicios la consultan vía <c>HttpAuthorizationPort</c>.
/// </summary>
/// <remarks>
/// Sin <c>[Authorize]</c> a propósito: es una llamada servicio-a-servicio y D6 decidió
/// explícitamente "sin client credentials internas en la POC". Queda como follow-up si una wave
/// futura necesita cerrar la red interna.
/// </remarks>
[ApiController]
[Route("api/v1/authorization")]
public sealed class AuthorizationController(IAuthorizationPort authorizationPort) : ControllerBase
{
    [HttpPost("evaluate")]
    public async Task<ActionResult<AuthorizationDecision>> Evaluate(
        [FromBody] AuthorizationEvaluationRequestV1 request,
        CancellationToken cancellationToken)
    {
        var decision = await authorizationPort.EvaluateAsync(
            request.ActorId,
            request.Permission,
            request.ResourceType,
            request.ResourceId,
            cancellationToken);

        return Ok(decision);
    }

    /// <summary>Matriz completa rol→permiso (evidencia requerida #1 de V2-ACL-001), para la screen ADM-03.</summary>
    [HttpGet("role-permission-matrix")]
    public ActionResult<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetRolePermissionMatrix() =>
        Ok(PermissionMatrix.AsDictionary());
}
