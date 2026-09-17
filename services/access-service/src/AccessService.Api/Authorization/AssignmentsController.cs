using Microsoft.AspNetCore.Mvc;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Authorization;

namespace AccessService.Api.Authorization;

/// <summary>
/// ASSIGN-001/ASSIGN-002: valida (sin persistir) una asignación de responsable propuesta por el
/// servicio owner del recurso (ej. <c>party-service</c>, V2-PTY-001). Ver
/// <see cref="IResponsibleAssignmentValidationPort"/> para la distinción entre <c>actorId</c>
/// (sub de Keycloak) y <c>responsibleUserId</c> (userId propio de access-service).
/// </summary>
/// <remarks>Sin <c>[Authorize]</c>, mismo motivo que <see cref="AuthorizationController"/> (D6).</remarks>
[ApiController]
[Route("api/v1/assignments")]
public sealed class AssignmentsController(IResponsibleAssignmentValidationPort validationPort) : ControllerBase
{
    [HttpPost("validate")]
    public async Task<ActionResult<AuthorizationDecision>> Validate(
        [FromBody] ResponsibleAssignmentValidationRequestV1 request,
        CancellationToken cancellationToken)
    {
        var decision = await validationPort.ValidateAsync(
            request.ActorId,
            request.ResourceType,
            request.ResourceId,
            request.ResponsibleUserId,
            cancellationToken);

        return Ok(decision);
    }
}
