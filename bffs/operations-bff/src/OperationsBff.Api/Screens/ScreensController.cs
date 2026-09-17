using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OperationsBff.Api.AccessService;

namespace OperationsBff.Api.Screens;

/// <summary>
/// <c>GET /screens/{screenId}</c> (D5, contrato ya usado por <c>apps/crm-web</c>). Implementa
/// solo el slice de administración de usuarios de V2-ACL-001: <c>ADM-01</c> (Usuarios),
/// <c>ADM-03</c> (Roles y permisos) y <c>ADM-04</c> (Permisos efectivos) — los 3 <c>screenId</c>
/// reales de <c>apps/crm-web/src/lib/screen-registry.ts</c> con <c>task: "V2-ACL-001"</c> que
/// corresponden a datos de acceso (no invitación de usuario, fuera de alcance de la POC).
/// </summary>
[ApiController]
[Route("screens")]
[Authorize]
public sealed class ScreensController(AccessServiceClient accessServiceClient) : ControllerBase
{
    [HttpGet("{screenId}")]
    public async Task<IActionResult> GetScreen(
        string screenId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 0,
        [FromQuery] Guid? userId = null)
    {
        if (screenId == "ADM-01")
        {
            var effectivePage = page <= 0 ? 1 : page;
            var effectivePageSize = pageSize <= 0 ? 20 : pageSize;
            var response = await accessServiceClient.GetAsync(
                $"/api/v1/users?page={effectivePage}&pageSize={effectivePageSize}",
                cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

        if (screenId == "ADM-03")
        {
            var response = await accessServiceClient.GetAsync("/api/v1/authorization/role-permission-matrix", cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

        if (screenId == "ADM-04")
        {
            if (userId is null)
            {
                return BadRequest("La screen ADM-04 requiere ?userId= (ver Follow-ups del reporte de V2-ACL-001).");
            }

            var response = await accessServiceClient.GetAsync($"/api/v1/users/{userId}/effective-permissions", cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

        return NotFound();
    }
}
