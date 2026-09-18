using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OperationsBff.Api.AccessService;
using OperationsBff.Api.PlatformConfigService;
using RealEstateCrm.Contracts.Catalogs;

namespace OperationsBff.Api.Screens;

/// <summary>
/// <c>GET /screens/{screenId}</c> (D5, contrato ya usado por <c>apps/crm-web</c>). Implementa el
/// slice de administración de usuarios de V2-ACL-001 (<c>ADM-01</c>/<c>ADM-03</c>/<c>ADM-04</c>)
/// y el slice de catálogos de V2-CAT-001 (<c>ADM-05</c>..<c>ADM-12</c>, los <c>screenId</c> reales
/// de <c>apps/crm-web/src/lib/screen-registry.ts</c> con <c>task: "V2-CAT-001"</c> que
/// corresponden a datos de catálogo).
/// </summary>
/// <remarks>
/// Un único controller con branching por <c>screenId</c> (D5 ya lo decidió así en V2-ACL-001, no
/// se reestructura acá, plan Wave 2 §7.3): dos controllers con <c>[Route("screens")]</c> +
/// <c>[HttpGet("{screenId}")]</c> serían rutas ambiguas para ASP.NET Core. V2-CAT-001 excluye
/// <c>AUT-04</c> (wizard de primer ingreso) y <c>ADM-13</c> (datos de la inmobiliaria, fuera de
/// alcance: no hay configuración de organización en V2, ADR-001) como overrides POC — mismo
/// criterio que V2-ACL-001 excluyó <c>ADM-02</c>.
/// </remarks>
[ApiController]
[Route("screens")]
[Authorize]
public sealed class ScreensController(
    AccessServiceClient accessServiceClient,
    PlatformConfigServiceClient platformConfigServiceClient) : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string> CatalogScreenTypes = new Dictionary<string, string>
    {
        ["ADM-06"] = CatalogTypes.CommercialStage,
        ["ADM-07"] = CatalogTypes.ActivityType,
        ["ADM-08"] = CatalogTypes.CommercialOrigin,
        ["ADM-09"] = CatalogTypes.LossReason,
        ["ADM-10"] = CatalogTypes.OperationType,
        ["ADM-11"] = CatalogTypes.PropertyType,
    };

    [HttpGet("{screenId}")]
    public async Task<IActionResult> GetScreen(
        string screenId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 0,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? catalogType = null,
        [FromQuery] bool activeOnly = false)
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

        if (screenId == "ADM-05")
        {
            // Índice de catálogos: navegación estática, no requiere ida a platform-config-service.
            return Ok(CatalogTypes.All.Select(type => new { catalogType = type }));
        }

        if (CatalogScreenTypes.TryGetValue(screenId, out var fixedCatalogType))
        {
            var response = await platformConfigServiceClient.GetAsync(
                $"/api/v1/catalogs/{fixedCatalogType}?activeOnly={activeOnly}",
                cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

        if (screenId == "ADM-12")
        {
            if (string.IsNullOrEmpty(catalogType))
            {
                return BadRequest("La screen ADM-12 requiere ?catalogType=.");
            }

            var response = await platformConfigServiceClient.GetAsync(
                $"/api/v1/catalogs/{catalogType}?activeOnly={activeOnly}",
                cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

        return NotFound();
    }
}
