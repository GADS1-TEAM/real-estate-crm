using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OperationsBff.Api.AccessService;
using OperationsBff.Api.PartyService;
using OperationsBff.Api.PlatformConfigService;
using RealEstateCrm.Contracts.Catalogs;
using OperationsBff.Api.PropertyService;
using OperationsBff.Api.SupplyService;

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
    PlatformConfigServiceClient platformConfigServiceClient,
    PartyServiceClient partyServiceClient,
    PropertyServiceClient propertyServiceClient,
    SupplyServiceClient supplyServiceClient) : ControllerBase
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
        [FromQuery] bool activeOnly = false,
        [FromQuery] Guid? entityId = null,
        [FromQuery] string? q = null,
        [FromQuery] string? kind = null,
        [FromQuery] string? commercialStatus = null,
        [FromQuery] Guid? responsibleUserId = null)
    {
        // V2-PTY-001 (party-service): screenId reales de screen-registry.ts con task "V2-PTY-001".
        // PTY-02/03/04 son formularios de alta (sin datos que leer) y PTY-12 (Historial) es de ACT.
        if (screenId is "PTY-01" or "GLB-11")
        {
            var effectivePage = page <= 0 ? 1 : page;
            var effectivePageSize = pageSize <= 0 ? 20 : pageSize;

            // GLB-11 (contenido archivado) = parties con baja lógica: commercialStatus INACTIVE.
            var status = screenId == "GLB-11" ? "INACTIVE" : commercialStatus;

            var query = $"page={effectivePage}&pageSize={effectivePageSize}";
            query += string.IsNullOrEmpty(q) ? string.Empty : $"&q={Uri.EscapeDataString(q)}";
            query += string.IsNullOrEmpty(kind) ? string.Empty : $"&kind={Uri.EscapeDataString(kind)}";
            query += string.IsNullOrEmpty(status) ? string.Empty : $"&commercialStatus={Uri.EscapeDataString(status)}";
            query += responsibleUserId is null ? string.Empty : $"&responsibleUserId={responsibleUserId}";

            var response = await partyServiceClient.GetAsync($"/api/v1/parties?{query}", cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

        if (screenId is "PTY-05" or "PTY-06" or "PTY-07" or "PTY-08" or "PTY-09" or "PTY-10" or "PTY-11" or "PTY-13")
        {
            if (entityId is null)
            {
                return BadRequest($"La screen {screenId} requiere ?entityId= (partyId).");
            }

            // PTY-05 = Contacto 360, PTY-06 = Empresa 360; el resto (relaciones, edición, estado,
            // baja, identidad técnica) opera sobre una Party de cualquiera de los dos tipos.
            var path = screenId switch
            {
                "PTY-05" => $"/api/v1/contacts/{entityId}",
                "PTY-06" => $"/api/v1/companies/{entityId}",
                _ => $"/api/v1/parties/{entityId}",
            };

            var response = await partyServiceClient.GetAsync(path, cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

        if (screenId is "PRP-01" or "PRP-10")
        {
            var effectivePage = page <= 0 ? 1 : page;
            var effectivePageSize = pageSize <= 0 ? 20 : pageSize;
            var query = $"page={effectivePage}&pageSize={effectivePageSize}";
            if (!string.IsNullOrEmpty(q)) query += $"&q={Uri.EscapeDataString(q)}";
            
            var response = await propertyServiceClient.GetAsync($"/api/v1/properties?{query}", cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }
        
        if (screenId is "PRP-03" or "PRP-04" or "PRP-05" or "PRP-06" or "PRP-13")
        {
            if (entityId is null) return BadRequest($"La screen {screenId} requiere ?entityId=.");
            var response = await propertyServiceClient.GetAsync($"/api/v1/properties/{entityId}", cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

        if (screenId is "LST-01" or "LST-09")
        {
            var effectivePage = page <= 0 ? 1 : page;
            var effectivePageSize = pageSize <= 0 ? 20 : pageSize;
            var query = $"page={effectivePage}&pageSize={effectivePageSize}";
            if (!string.IsNullOrEmpty(q)) query += $"&q={Uri.EscapeDataString(q)}";
            
            var response = await supplyServiceClient.GetAsync($"/api/v1/listings?{query}", cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

        if (screenId is "LST-02" or "LST-04" or "LST-05" or "LST-07")
        {
            if (entityId is null) return BadRequest($"La screen {screenId} requiere ?entityId=.");
            var response = await supplyServiceClient.GetAsync($"/api/v1/listings/{entityId}", cancellationToken);
            return await ProxyResults.FromAsync(response, cancellationToken);
        }

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
