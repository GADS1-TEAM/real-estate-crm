using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OperationsBff.Api.AccessService;
using OperationsBff.Api.PartyService;
using OperationsBff.Api.PlatformConfigService;
using RealEstateCrm.Contracts.Catalogs;
using OperationsBff.Api.PropertyService;
using OperationsBff.Api.SupplyService;
using OperationsBff.Api.ActivityService;
using OperationsBff.Api.AnalyticsService;
using OperationsBff.Api.AutomationAiService;
using OperationsBff.Api.CommercialService;
using OperationsBff.Api.DemandService;
using OperationsBff.Api.MatchingService;
using System.Text.Json;

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
[AllowAnonymous]
public sealed class ScreensController(
    AccessServiceClient accessServiceClient,
    PlatformConfigServiceClient platformConfigServiceClient,
    PartyServiceClient partyServiceClient,
    PropertyServiceClient propertyServiceClient,
    SupplyServiceClient supplyServiceClient,
    DemandServiceClient demandServiceClient,
    MatchingServiceClient matchingServiceClient,
    CommercialServiceClient commercialServiceClient,
    ActivityServiceClient activityServiceClient,
    AnalyticsServiceClient analyticsServiceClient,
    AutomationAiServiceClient automationAiServiceClient) : ControllerBase
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
        var demoState = await BuildDemoStateAsync(screenId, catalogType, activeOnly, cancellationToken);
        return Ok(demoState);
    }

    private async Task<object> BuildDemoStateAsync(
        string screenId, 
        string? queryCatalogType, 
        bool activeOnly, 
        CancellationToken cancellationToken)
    {
        var contactsTask = SafeFetchArrayAsync(partyServiceClient.GetAsync, "/api/v1/parties?page=1&pageSize=50", cancellationToken);
        var propertiesTask = SafeFetchArrayAsync(propertyServiceClient.GetAsync, "/api/v1/properties?page=1&pageSize=50", cancellationToken);
        var listingsTask = SafeFetchArrayAsync(supplyServiceClient.GetAsync, "/api/v1/listings?page=1&pageSize=50", cancellationToken);
        var demandsTask = SafeFetchArrayAsync(demandServiceClient.GetAsync, "/api/v1/requirements", cancellationToken);
        var usersTask = SafeFetchArrayAsync(accessServiceClient.GetAsync, "/api/v1/users?page=1&pageSize=50", cancellationToken);
        var activitiesTask = SafeFetchArrayAsync(activityServiceClient.GetAsync, "/api/v1/activities/timeline", cancellationToken);
        var opportunitiesTask = SafeFetchArrayAsync(commercialServiceClient.GetAsync, "/api/v1/opportunities", cancellationToken);

        Task<JsonElement> catalogTask;
        if (screenId == "ADM-05")
        {
            var docs = JsonSerializer.SerializeToDocument(CatalogTypes.All.Select(type => new { catalogType = type }));
            catalogTask = Task.FromResult(docs.RootElement);
        }
        else if (CatalogScreenTypes.TryGetValue(screenId, out var fixedType))
        {
            catalogTask = SafeFetchArrayAsync(platformConfigServiceClient.GetAsync, $"/api/v1/catalogs/{fixedType}?activeOnly={activeOnly}", cancellationToken);
        }
        else if (screenId == "ADM-12" && !string.IsNullOrEmpty(queryCatalogType))
        {
            catalogTask = SafeFetchArrayAsync(platformConfigServiceClient.GetAsync, $"/api/v1/catalogs/{queryCatalogType}?activeOnly={activeOnly}", cancellationToken);
        }
        else
        {
            catalogTask = FetchAllCatalogsAsync(activeOnly, cancellationToken);
        }

        await Task.WhenAll(
            contactsTask, propertiesTask, listingsTask, demandsTask, 
            usersTask, activitiesTask, opportunitiesTask, catalogTask);

        return new
        {
            contacts = contactsTask.Result,
            properties = propertiesTask.Result,
            opportunities = opportunitiesTask.Result,
            listings = listingsTask.Result,
            captations = Array.Empty<object>(),
            demands = demandsTask.Result,
            activities = activitiesTask.Result,
            reservations = Array.Empty<object>(),
            operations = Array.Empty<object>(),
            proposals = Array.Empty<object>(),
            proposal = new { id = "", contact = "", property = "", amount = 0, currency = "USD", date = "", conditions = "", expiration = "", status = "pending" },
            stageHistory = Array.Empty<object>(),
            offlineQueue = Array.Empty<object>(),
            matchActions = Array.Empty<object>(),
            aiSuggestions = Array.Empty<object>(),
            catalogEntries = catalogTask.Result,
            users = usersTask.Result,
            catalogVersion = 1
        };
    }

    private async Task<JsonElement> FetchAllCatalogsAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var types = new[] { "pipeline-stage", "activity-type", "commercial-origin", "loss-reason", "operation-type", "property-type" };
        var tasks = types.Select(t => SafeFetchArrayAsync(platformConfigServiceClient.GetAsync, $"/api/v1/catalogs/{t}?activeOnly={activeOnly}", cancellationToken)).ToList();
        
        await Task.WhenAll(tasks);
        
        var allEntries = tasks.SelectMany(t => 
        {
            if (t.Result.ValueKind == JsonValueKind.Array)
            {
                return (IEnumerable<JsonElement>)t.Result.EnumerateArray();
            }
            return Array.Empty<JsonElement>();
        }).ToList();

        var docs = JsonSerializer.SerializeToDocument(allEntries);
        return docs.RootElement;
    }

    private async Task<JsonElement> SafeFetchArrayAsync(
        Func<string, CancellationToken, Task<HttpResponseMessage>> getAsync, 
        string path, 
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await getAsync(path, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                
                if (root.ValueKind == JsonValueKind.Array) return root.Clone();
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array) return items.Clone();
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array) return data.Clone();
                
                return root.Clone();
            }
        }
        catch { }
        
        return JsonDocument.Parse("[]").RootElement;
    }
}
