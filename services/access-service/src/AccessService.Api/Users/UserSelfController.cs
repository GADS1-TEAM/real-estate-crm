using AccessService.Api.ErrorHandling;
using AccessService.Api.ExecutionContextResolution;
using AccessService.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessService.Api.Users;

/// <summary>
/// <c>GET /api/v1/users/me</c>: el usuario de negocio del propio token (<c>UserSelfV1</c>). Contrato
/// nuevo y aditivo de V2-PTY-001: lo consumen los owners (ej. party-service) vía
/// <c>IUserDirectoryPort</c> para traducir <c>sub</c> → <c>userId</c>. Resuelve siempre desde el
/// token: no acepta ningún id, así que no expone búsqueda por <c>sub</c> de terceros. No exige un
/// permiso de negocio (todo usuario puede saber quién es); un usuario PENDING/INACTIVE recibe 403
/// con el mismo <c>reasonCode</c> que usa la autorización.
/// </summary>
[ApiController]
[Route("api/v1/users/me")]
public sealed class UserSelfController(
    UserSelfService userSelfService,
    CurrentExecutionContextProvider executionContextProvider) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSelf(CancellationToken cancellationToken)
    {
        var context = await executionContextProvider.GetAsync(cancellationToken);
        if (context is null)
        {
            return ProblemDetailsResults.Unauthorized("Token ausente o inválido.", Guid.NewGuid(), Request.Path);
        }

        var result = await userSelfService.GetSelfAsync(context.ActorId, cancellationToken);

        if (!result.IsActive)
        {
            return ProblemDetailsResults.Forbidden(result.DenyReasonCode!, context.CorrelationId, Request.Path);
        }

        return Ok(result.User);
    }
}
