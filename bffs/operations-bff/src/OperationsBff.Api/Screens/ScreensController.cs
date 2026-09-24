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

[ApiController]
[Route("screens")]
[AllowAnonymous]
[ServiceFilter(typeof(OperationsBff.Api.Authentication.RequireSessionOutsideDevelopmentFilter))]
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
    AutomationAiServiceClient automationAiServiceClient,
    PipelineProjectionStore pipelineProjectionStore) : ControllerBase
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
        var usersTask = SafeFetchUsersAsync(cancellationToken);
        var activitiesTask = SafeFetchActivitiesAsync(cancellationToken);
        var opportunitiesTask = pipelineProjectionStore.GetOpportunitiesAsync(cancellationToken);
        var stageHistoryTask = pipelineProjectionStore.GetStageHistoryAsync(cancellationToken);
        var proposalsTask = pipelineProjectionStore.GetProposalsAsync(cancellationToken);
        var reservationsTask = pipelineProjectionStore.GetReservationsAsync(cancellationToken);
        var relationshipsTask = pipelineProjectionStore.GetRelationshipsAsync(cancellationToken);

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
            usersTask, activitiesTask, opportunitiesTask, stageHistoryTask,
            proposalsTask, reservationsTask, relationshipsTask, catalogTask);

        var opportunities = opportunitiesTask.Result.Select(o => new
        {
            id = o.Id,
            title = o.Title,
            sourceType = o.SourceType,
            sourceId = o.SourceId,
            stage = o.Stage,
            owner = o.Owner,
            fee = o.Fee,
            currency = o.Currency,
            daysInStage = o.DaysInStage,
            origin = o.Origin ?? "Carga manual",
            outcome = o.Outcome,
            closedAt = o.ClosedAt,
            finalValue = o.FinalValue,
            closeReason = o.CloseReason
        }).ToList();

        var stageHistory = stageHistoryTask.Result.Select(h => new
        {
            id = h.Id,
            opportunityId = h.OpportunityId,
            from = h.From,
            to = h.To,
            actor = h.Actor,
            at = h.At,
            reason = h.Reason,
            outcome = h.Outcome,
            closeDate = h.CloseDate,
            finalValue = h.FinalValue
        }).ToList();

        var proposals = proposalsTask.Result.Select(p => new
        {
            id = p.Id,
            opportunityId = p.OpportunityId,
            sequence = p.Sequence,
            kind = p.Kind,
            proposedBy = p.ProposedBy,
            proposedTo = p.ProposedTo,
            amount = p.Amount,
            currency = p.Currency,
            validity = p.Validity,
            conditions = p.Conditions,
            outcome = p.Outcome,
            reason = p.Reason,
            createdAt = p.CreatedAt,
            respondedAt = p.RespondedAt
        }).ToList();

        var reservations = reservationsTask.Result.Select(r => new
        {
            id = r.Id,
            opportunityId = r.OpportunityId,
            propertyTitle = r.PropertyTitle,
            deposit = r.Deposit,
            currency = r.Currency,
            status = r.Status,
            conditions = r.Conditions
        }).ToList();

        var relationships = relationshipsTask.Result.Select(rel => new
        {
            id = rel.Id,
            fromPartyId = rel.FromPartyId,
            toPartyId = rel.ToPartyId,
            relationshipType = rel.RelationshipType,
            createdAt = rel.CreatedAt,
            createdBy = rel.CreatedBy
        }).ToList();

        return new
        {
            contacts = contactsTask.Result,
            relationships,
            properties = propertiesTask.Result,
            opportunities,
            listings = listingsTask.Result,
            captations = GetDefaultCaptations(),
            demands = demandsTask.Result,
            activities = activitiesTask.Result,
            reservations,
            operations = new[]
            {
                new { id = "operation-1", reservationId = "reservation-1", propertyTitle = "PH en Guardia Vieja 3355", propertyId = "PROP-101", listingId = "LST-101", operationType = "Venta", stage = "Documentación", outcome = "OPEN" }
            },
            proposals,
            proposal = new { outcome = "pending", reason = "" },
            stageHistory,
            offlineQueue = Array.Empty<object>(),
            matchActions = Array.Empty<object>(),
            aiSuggestions = new[]
            {
                new { id = "ai-surface", title = "Completar criterio de superficie", body = "Ana mencionó que busca ambientes, pero la superficie cubierta todavía es desconocida.", evidence = "WhatsApp histórico · confianza media", status = "pending" },
                new { id = "ai-match", title = "Presentar Casa en Villa Crespo", body = "La publicación coincide con barrio y ambientes de una búsqueda activa.", evidence = "2 coincidencias · 2 datos desconocidos", status = "pending" }
            },
            catalogEntries = catalogTask.Result,
            users = usersTask.Result,
            catalogVersion = 1
        };
    }

    private static object GetDefaultCaptations()
    {
        return new[]
        {
            new { id = "captation-1", propertyId = "PROP-101", owner = "Lucía Ferrari", stage = "Mandato", expectation = 245000, valuation = (decimal?)235000, currency = "USD", origin = "Referido" },
            new { id = "captation-2", propertyId = "PROP-102", owner = "Lucía Ferrari", stage = "Tasación", expectation = 410000, valuation = (decimal?)null, currency = "USD", origin = "Carga manual" },
            new { id = "cap-101", propertyId = "PROP-101", owner = "Martín Quiroga", stage = "Nueva", expectation = 180000, valuation = (decimal?)175000, currency = "USD", origin = "Portal inmobiliario" },
            new { id = "cap-102", propertyId = "PROP-102", owner = "Lucía Ferrari", stage = "Tasación", expectation = 550000, valuation = (decimal?)540000, currency = "USD", origin = "Portal inmobiliario" },
            new { id = "cap-103", propertyId = "PROP-103", owner = "Rodrigo Vergara", stage = "Mandato", expectation = 2800, valuation = (decimal?)2500, currency = "USD", origin = "Referido" },
            new { id = "cap-104", propertyId = "PROP-104", owner = "Martín Quiroga", stage = "Publicada", expectation = 950000, valuation = (decimal?)920000, currency = "USD", origin = "Carga manual" }
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

                var transformed = items
                    .Select(item =>
                    {
                        var id = TryGetStringProperty(item, "partyId") ?? TryGetStringProperty(item, "id") ?? "";
                        var name = TryGetStringProperty(item, "displayName") ?? TryGetStringProperty(item, "name") ?? "";
                        var rawKind = TryGetStringProperty(item, "kind") ?? "";
                        var kind = rawKind is "LEGAL_ENTITY" or "Empresa" ? "Empresa" : "Persona";
                        var email = TryGetStringProperty(item, "email") ?? "";
                        var phone = TryGetStringProperty(item, "phone") ?? "";
                        var commercialStatus = TryGetStringProperty(item, "commercialStatus") ?? "POTENTIAL";
                        var identityStatus = TryGetStringProperty(item, "identityStatus") ?? "ACTIVE";
                        var status = identityStatus == "INACTIVE" ? "Archivado" : "Activo";
                        var owner = kind == "Empresa" ? "Sofía Rendón" : "Martín Quiroga";
                        var origin = TryGetStringProperty(item, "originCode") ?? "Carga manual";

                        return new { id, name, kind, phone, email, status, owner, commercialStatus, identityStatus, origin };
                    })
                    .Where(x => !string.Equals(x.name, "asd", StringComparison.OrdinalIgnoreCase) && !string.Equals(x.name, "ramiro", StringComparison.OrdinalIgnoreCase))
                    .ToList();

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
        var baseline = new List<object>
        {
            new
            {
                id = "demand-1",
                contactId = "contact-1",
                title = "Ana busca casa en Villa Crespo",
                status = "Activa",
                criteria = new object[]
                {
                    new { id = "rooms", label = "Ambientes", value = "3", weight = "must" },
                    new { id = "neighborhood", label = "Barrio", value = "Villa Crespo", weight = "nice" },
                    new { id = "surface", label = "Superficie cubierta", value = "UNKNOWN", weight = "unknown" }
                },
                score = 82,
                origin = "WhatsApp histórico",
                selectedListingIds = new[] { "LST-101" }
            },
            new
            {
                id = "demand-2",
                contactId = "contact-2",
                title = "Carla busca PH con terraza",
                status = "Activa",
                criteria = new object[]
                {
                    new { id = "rooms", label = "Ambientes", value = "4", weight = "must" },
                    new { id = "outdoor", label = "Terraza", value = "Sí", weight = "nice" }
                },
                score = 74,
                origin = "Referido",
                selectedListingIds = Array.Empty<string>()
            }
        };

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
                    var origin = TryGetStringProperty(item, "originCode") ?? "Portal inmobiliario";

                    var criteria = new object[]
                    {
                        new { id = "op", label = "Operación", value = opType, weight = "must" },
                        new { id = "prop", label = "Tipo Propiedad", value = propType, weight = "must" },
                        new { id = "neighborhood", label = "Barrio", value = neighborhood, weight = "nice" },
                        new { id = "surface", label = "Superficie cubierta", value = "UNKNOWN", weight = "unknown" }
                    };

                    return (object)new { id, contactId, title, status = "Activa", criteria, score = 85, origin, selectedListingIds = Array.Empty<string>() };
                }).ToList();

                baseline.AddRange(transformed);
            }
        }
        catch { }

        var docs = JsonSerializer.SerializeToDocument(baseline);
        return docs.RootElement;
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
                    var rawType = TryGetStringProperty(item, "activityTypeCode") ?? TryGetStringProperty(item, "type") ?? "NOTE";
                    var type = rawType switch
                    {
                        "VISIT" or "Visita" => "Visita",
                        "CALL" or "Llamada" => "Llamada",
                        "MEETING" or "Reunión" => "Reunión presencial",
                        "EMAIL" or "Email" => "Email",
                        _ => "Nota"
                    };
                    var description = TryGetStringProperty(item, "description") ?? TryGetStringProperty(item, "summary") ?? TryGetStringProperty(item, "text") ?? "Registro de actividad";
                    var result = TryGetStringProperty(item, "result") ?? "";
                    var body = !string.IsNullOrEmpty(result) ? $"{description} — Resultado: {result}" : description;
                    var subject = $"{type}: {description}";
                    var actor = "Martín Quiroga";
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

    private async Task<JsonElement> SafeFetchUsersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await accessServiceClient.GetAsync("/api/v1/users?page=1&pageSize=50", cancellationToken);
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
                    var id = TryGetStringProperty(item, "userId") ?? TryGetStringProperty(item, "id") ?? Guid.NewGuid().ToString();
                    var name = TryGetStringProperty(item, "displayName") ?? TryGetStringProperty(item, "name") ?? "Usuario";
                    var email = TryGetStringProperty(item, "email") ?? "usuario@inmobiliaria.com";
                    var role = "Vendedor";
                    if (item.TryGetProperty("roleCodes", out var rc) && rc.ValueKind == JsonValueKind.Array && rc.GetArrayLength() > 0)
                    {
                        var firstRole = rc[0].GetString() ?? "";
                        if (firstRole.Contains("Responsable", StringComparison.OrdinalIgnoreCase)) role = "Responsable comercial";
                        else if (firstRole.Contains("Admin", StringComparison.OrdinalIgnoreCase)) role = "Administradora";
                    }
                    else
                    {
                        role = TryGetStringProperty(item, "role") ?? "Vendedor";
                    }
                    var status = "Habilitado";

                    return new { id, name, email, role, status };
                }).ToList();

                if (transformed.Count > 0)
                {
                    var docs = JsonSerializer.SerializeToDocument(transformed);
                    return docs.RootElement;
                }
            }
        }
        catch { }

        var defaultUsers = new[]
        {
            new { id = "usr-1", name = "Martín Quiroga", email = "martin@inmobiliaria.com.ar", role = "Vendedor", status = "Habilitado" },
            new { id = "usr-2", name = "Rodrigo Vergara", email = "rodrigo@inmobiliaria.com.ar", role = "Responsable comercial", status = "Habilitado" },
            new { id = "usr-3", name = "Lucía Ferrari", email = "lucia@inmobiliaria.com.ar", role = "Vendedor", status = "Habilitado" },
            new { id = "usr-4", name = "Sofía Rendón", email = "sofia@inmobiliaria.com.ar", role = "Administradora", status = "Habilitado" }
        };

        var docsDefault = JsonSerializer.SerializeToDocument(defaultUsers);
        return docsDefault.RootElement;
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
