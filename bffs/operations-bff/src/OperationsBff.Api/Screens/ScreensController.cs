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
        var contactsTask = SafeFetchContactsAsync(cancellationToken);
        var propertiesTask = SafeFetchPropertiesAsync(cancellationToken);
        var listingsTask = SafeFetchListingsAsync(cancellationToken);
        var demandsTask = SafeFetchDemandsAsync(cancellationToken);
        var usersTask = SafeFetchArrayAsync(accessServiceClient.GetAsync, "/api/v1/users?page=1&pageSize=50", cancellationToken);
        var activitiesTask = SafeFetchActivitiesAsync(cancellationToken);
        var opportunitiesTask = SafeFetchOpportunitiesAsync(cancellationToken);

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
            captations = GetDefaultCaptations(),
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

    private static object GetDefaultCaptations()
    {
        return new[]
        {
            new { id = "cap-101", propertyId = "PROP-101", owner = "Martin Quiroga", stage = "Nueva", expectation = 180000, valuation = (decimal?)175000, currency = "USD", origin = "WEB" },
            new { id = "cap-102", propertyId = "PROP-102", owner = "Lucía Ferrari", stage = "Tasación", expectation = 550000, valuation = (decimal?)540000, currency = "USD", origin = "PORTAL" },
            new { id = "cap-103", propertyId = "PROP-103", owner = "Rodrigo Vergara", stage = "Mandato", expectation = 2800, valuation = (decimal?)2500, currency = "USD", origin = "RECOMMENDATION" },
            new { id = "cap-104", propertyId = "PROP-104", owner = "Martin Quiroga", stage = "Publicada", expectation = 950000, valuation = (decimal?)920000, currency = "USD", origin = "DIRECT" }
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

    private async Task<JsonElement> SafeFetchContactsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await partyServiceClient.GetAsync("/api/v1/parties?page=1&pageSize=100", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                IEnumerable<JsonElement> items = Array.Empty<JsonElement>();
                if (root.ValueKind == JsonValueKind.Array) items = root.EnumerateArray();
                else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array) items = itemsProp.EnumerateArray().ToList();

                var transformed = items.Select(item =>
                {
                    var id = TryGetStringProperty(item, "partyId") ?? TryGetStringProperty(item, "id") ?? "";
                    var name = TryGetStringProperty(item, "displayName") ?? TryGetStringProperty(item, "name") ?? "";
                    var rawKind = TryGetStringProperty(item, "kind") ?? "";
                    var kind = rawKind is "LEGAL_ENTITY" or "Empresa" ? "Empresa" : "Persona";
                    var email = TryGetStringProperty(item, "email") ?? "";
                    var phone = TryGetStringProperty(item, "phone") ?? "";
                    var commercialStatus = TryGetStringProperty(item, "commercialStatus") ?? "LEAD";
                    var identityStatus = TryGetStringProperty(item, "identityStatus") ?? "ACTIVE";
                    var status = identityStatus == "INACTIVE" ? "Archivado" : "Activo";
                    var owner = "Usuario actual";
                    var origin = TryGetStringProperty(item, "originCode") ?? "";

                    return new { id, name, kind, phone, email, status, owner, commercialStatus, identityStatus, origin };
                }).ToList();

                var docs = JsonSerializer.SerializeToDocument(transformed);
                return docs.RootElement;
            }
        }
        catch { }

        return JsonDocument.Parse("[]").RootElement;
    }

    private async Task<JsonElement> SafeFetchPropertiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await propertyServiceClient.GetAsync("/api/v1/properties?page=1&pageSize=50", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                IEnumerable<JsonElement> items = Array.Empty<JsonElement>();
                if (root.ValueKind == JsonValueKind.Array) items = root.EnumerateArray();
                else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array) items = itemsProp.EnumerateArray().ToList();

                var transformed = items.Select(item =>
                {
                    var id = TryGetStringProperty(item, "propertyId") ?? TryGetStringProperty(item, "id") ?? Guid.NewGuid().ToString();
                    var title = TryGetStringProperty(item, "title") ?? "Inmueble";
                    
                    string address = "Capital Federal";
                    if (item.TryGetProperty("addressStreet", out var street))
                    {
                        var num = TryGetStringProperty(item, "addressNumber") ?? "";
                        address = $"{street.GetString()} {num}".Trim();
                    }
                    else
                    {
                        address = TryGetStringProperty(item, "address") ?? "Capital Federal";
                    }

                    var type = TryGetStringProperty(item, "propertyTypeCode") ?? TryGetStringProperty(item, "type") ?? "Departamento";
                    var bedrooms = item.TryGetProperty("bedrooms", out var b) ? b.ToString() : "3";
                    
                    decimal price = 0;
                    string currency = "USD";
                    if (item.TryGetProperty("priceAmount", out var pa) && pa.TryGetDecimal(out var pad)) price = pad;
                    if (item.TryGetProperty("priceCurrency", out var pc)) currency = pc.GetString() ?? "USD";

                    return new { id, title, address, type, status = "Disponible", price, currency, bedrooms, geo = "Conocida", surfaceM2 = (int?)85, ownerPartyIds = Array.Empty<string>() };
                }).ToList();

                var docs = JsonSerializer.SerializeToDocument(transformed);
                return docs.RootElement;
            }
        }
        catch { }

        return JsonDocument.Parse("[]").RootElement;
    }

    private async Task<JsonElement> SafeFetchListingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await supplyServiceClient.GetAsync("/api/v1/listings?page=1&pageSize=50", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                IEnumerable<JsonElement> items = Array.Empty<JsonElement>();
                if (root.ValueKind == JsonValueKind.Array) items = root.EnumerateArray();
                else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array) items = itemsProp.EnumerateArray().ToList();

                var transformed = items.Select(item =>
                {
                    var id = TryGetStringProperty(item, "listingId") ?? TryGetStringProperty(item, "id") ?? Guid.NewGuid().ToString();
                    var propertyId = TryGetStringProperty(item, "propertyId") ?? "";
                    var title = TryGetStringProperty(item, "canonicalTitle") ?? TryGetStringProperty(item, "title") ?? "Publicación Comercial";
                    var description = TryGetStringProperty(item, "canonicalDescription") ?? TryGetStringProperty(item, "description") ?? "";
                    var operationType = TryGetStringProperty(item, "operationTypeCode") ?? "Venta";

                    decimal price = 0;
                    string currency = "USD";
                    if (item.TryGetProperty("commercialTerms", out var ct) && ct.ValueKind == JsonValueKind.Object)
                    {
                        if (ct.TryGetProperty("price", out var pr) && pr.ValueKind == JsonValueKind.Object)
                        {
                            if (pr.TryGetProperty("amount", out var am) && am.TryGetDecimal(out var amd)) price = amd;
                            if (pr.TryGetProperty("currency", out var cur)) currency = cur.GetString() ?? "USD";
                        }
                    }

                    return new { id, title, propertyId, status = "Activa", mandate = "Firmado", views = 42, description, price, currency, operationType };
                }).ToList();

                var docs = JsonSerializer.SerializeToDocument(transformed);
                return docs.RootElement;
            }
        }
        catch { }

        return JsonDocument.Parse("[]").RootElement;
    }

    private async Task<JsonElement> SafeFetchDemandsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await demandServiceClient.GetAsync("/api/v1/requirements", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                IEnumerable<JsonElement> items = Array.Empty<JsonElement>();
                if (root.ValueKind == JsonValueKind.Array) items = root.EnumerateArray();
                else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array) items = itemsProp.EnumerateArray().ToList();

                var transformed = items.Select(item =>
                {
                    var id = TryGetStringProperty(item, "requirementId") ?? TryGetStringProperty(item, "id") ?? Guid.NewGuid().ToString();
                    var contactId = "";
                    if (item.TryGetProperty("seekerPartyIds", out var seekers) && seekers.ValueKind == JsonValueKind.Array && seekers.GetArrayLength() > 0)
                    {
                        contactId = seekers[0].GetString() ?? "";
                    }
                    var opType = TryGetStringProperty(item, "operationTypeCode") ?? "Venta";
                    var propType = TryGetStringProperty(item, "propertyTypeCode") ?? "Departamento";
                    
                    string neighborhood = "Palermo";
                    if (item.TryGetProperty("locationCriteria", out var loc) && loc.ValueKind == JsonValueKind.Object)
                    {
                        neighborhood = TryGetStringProperty(loc, "neighborhood") ?? TryGetStringProperty(loc, "locality") ?? "Palermo";
                    }

                    var title = TryGetStringProperty(item, "title") ?? $"Búsqueda {opType} {propType} en {neighborhood}";
                    var origin = TryGetStringProperty(item, "originCode") ?? "WEB";

                    var criteria = new List<object>
                    {
                        new { id = "op", label = "Operación", value = opType, weight = "must" },
                        new { id = "prop", label = "Tipo Propiedad", value = propType, weight = "must" },
                        new { id = "neighborhood", label = "Barrio", value = neighborhood, weight = "nice" },
                        new { id = "surface", label = "Superficie cubierta", value = "UNKNOWN", weight = "unknown" }
                    };

                    return new { id, contactId, title, status = "Activa", criteria, score = 85, origin, selectedListingIds = Array.Empty<string>() };
                }).ToList();

                var docs = JsonSerializer.SerializeToDocument(transformed);
                return docs.RootElement;
            }
        }
        catch { }

        return JsonDocument.Parse("[]").RootElement;
    }

    private async Task<JsonElement> SafeFetchOpportunitiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await commercialServiceClient.GetAsync("/api/v1/opportunities", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                IEnumerable<JsonElement> items = Array.Empty<JsonElement>();
                if (root.ValueKind == JsonValueKind.Array) items = root.EnumerateArray();
                else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array) items = itemsProp.EnumerateArray().ToList();

                var transformed = items.Select(item =>
                {
                    var id = TryGetStringProperty(item, "opportunityId") ?? TryGetStringProperty(item, "id") ?? Guid.NewGuid().ToString();
                    var title = TryGetStringProperty(item, "title") ?? "Oportunidad Comercial";
                    var sourceType = TryGetStringProperty(item, "sourceType") ?? "REQUIREMENT";
                    var sourceId = TryGetStringProperty(item, "sourceId") ?? "";
                    var stage = TryGetStringProperty(item, "stageCode") ?? TryGetStringProperty(item, "stage") ?? "Nuevo";
                    var owner = TryGetStringProperty(item, "responsibleUserId") ?? "Martin Quiroga";
                    var origin = TryGetStringProperty(item, "originCode") ?? "WEB";

                    decimal fee = 0;
                    string currency = "USD";
                    if (item.TryGetProperty("estimatedFee", out var ef) && ef.ValueKind == JsonValueKind.Object)
                    {
                        if (ef.TryGetProperty("amount", out var am) && am.TryGetDecimal(out var amd)) fee = amd;
                        if (ef.TryGetProperty("currency", out var cur)) currency = cur.GetString() ?? "USD";
                    }

                    return new { id, title, sourceType, sourceId, stage, owner, fee, currency, daysInStage = 3, origin };
                }).ToList();

                if (transformed.Count > 0)
                {
                    var docs = JsonSerializer.SerializeToDocument(transformed);
                    return docs.RootElement;
                }
            }
        }
        catch { }

        var defaultOpps = new List<object>
        {
            new { id = "opp-101", title = "Búsqueda Dpto Palermo (Juan Pérez)", sourceType = "REQUIREMENT", sourceId = "22222222-1111-4000-8000-000000000001", stage = "Nuevo", owner = "Martin Quiroga", fee = 6000, currency = "USD", daysInStage = 2, origin = "WEB" },
            new { id = "opp-102", title = "Venta Casa San Isidro (María Gonzalez)", sourceType = "REQUIREMENT", sourceId = "22222222-1111-4000-8000-000000000002", stage = "Contacto", owner = "Lucía Ferrari", fee = 18000, currency = "USD", daysInStage = 4, origin = "PORTAL" },
            new { id = "opp-103", title = "Alquiler Oficina Retiro (Inversiones SA)", sourceType = "REQUIREMENT", sourceId = "22222222-1111-4000-8000-000000000003", stage = "Visita", owner = "Rodrigo Vergara", fee = 2500, currency = "USD", daysInStage = 1, origin = "RECOMMENDATION" },
            new { id = "opp-104", title = "Venta Penthouse Puerto Madero (Carlos Ruiz)", sourceType = "REQUIREMENT", sourceId = "22222222-1111-4000-8000-000000000004", stage = "Negociación", owner = "Martin Quiroga", fee = 30000, currency = "USD", daysInStage = 6, origin = "DIRECT" },
            new { id = "opp-105", title = "Venta Lote Nordelta (Ana Martínez)", sourceType = "REQUIREMENT", sourceId = "22222222-1111-4000-8000-000000000005", stage = "Reserva", owner = "Lucía Ferrari", fee = 7500, currency = "USD", daysInStage = 3, origin = "WEB" },
            new { id = "opp-106", title = "Venta Depto Recoleta (Estudio Jurídico)", sourceType = "REQUIREMENT", sourceId = "LST-106", stage = "Operación", owner = "Rodrigo Vergara", fee = 5400, currency = "USD", daysInStage = 5, origin = "DIRECT" }
        };

        var docsDefault = JsonSerializer.SerializeToDocument(defaultOpps);
        return docsDefault.RootElement;
    }

    private async Task<JsonElement> SafeFetchActivitiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await activityServiceClient.GetAsync("/api/v1/activities/timeline", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                IEnumerable<JsonElement> items = Array.Empty<JsonElement>();
                if (root.ValueKind == JsonValueKind.Array) items = root.EnumerateArray();
                else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array) items = itemsProp.EnumerateArray().ToList();

                var transformed = items.Select(item =>
                {
                    var id = TryGetStringProperty(item, "activityId") ?? TryGetStringProperty(item, "id") ?? Guid.NewGuid().ToString();
                    var type = TryGetStringProperty(item, "activityTypeCode") ?? TryGetStringProperty(item, "type") ?? "Nota";
                    var description = TryGetStringProperty(item, "description") ?? TryGetStringProperty(item, "summary") ?? TryGetStringProperty(item, "text") ?? "Registro de actividad";
                    var result = TryGetStringProperty(item, "result") ?? "";
                    var body = !string.IsNullOrEmpty(result) ? $"{description} — Resultado: {result}" : description;
                    var subject = $"{type}: {description}";
                    var actor = "Martin Quiroga";
                    var occurredAt = TryGetStringProperty(item, "occurredAt") ?? DateTime.UtcNow.ToString("o");
                    var createdAt = TryGetStringProperty(item, "createdAt") ?? occurredAt;

                    return new { id, type, subject, body, actor, text = body, createdBy = actor, occurredAt, createdAt, historical = false, relatedRecordType = (string?)null, relatedRecordId = (string?)null };
                }).ToList();

                var docs = JsonSerializer.SerializeToDocument(transformed);
                return docs.RootElement;
            }
        }
        catch { }

        return JsonDocument.Parse("[]").RootElement;
    }

    private static string? TryGetStringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }
}
