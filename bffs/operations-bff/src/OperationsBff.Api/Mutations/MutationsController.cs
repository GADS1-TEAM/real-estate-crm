using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OperationsBff.Api.AccessService;
using OperationsBff.Api.ActivityService;
using OperationsBff.Api.AnalyticsService;
using OperationsBff.Api.AutomationAiService;
using OperationsBff.Api.CommercialService;
using OperationsBff.Api.DemandService;
using OperationsBff.Api.MatchingService;
using OperationsBff.Api.PartyService;
using OperationsBff.Api.PlatformConfigService;
using OperationsBff.Api.PropertyService;
using OperationsBff.Api.SupplyService;

namespace OperationsBff.Api.Mutations;

/// <summary>
/// <c>POST /mutations/{name}</c> (D5). Implementa las 4 mutaciones de administración de
/// usuarios de V2-ACL-001 (<c>createUser</c>/<c>updateUser</c>/<c>deactivateUser</c>/
/// <c>assignRole</c>, hacia access-service) y las 4 de catálogos de V2-CAT-001
/// (<c>createCatalogEntry</c>/<c>updateCatalogEntry</c>/<c>deactivateCatalogEntry</c>/
/// <c>publishCatalogVersion</c>, hacia platform-config-service). El body se reenvía tal cual al
/// servicio owner (que ignora campos extra usados acá solo para armar la ruta, ej.
/// <c>entryId</c>/<c>catalogType</c> en el body de <c>deactivateCatalogEntry</c>/
/// <c>publishCatalogVersion</c>).
/// </summary>
[ApiController]
[Route("mutations")]
[AllowAnonymous]
[ServiceFilter(typeof(OperationsBff.Api.Authentication.RequireSessionOutsideDevelopmentFilter))]
public sealed class MutationsController(
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
    [HttpPost("{name}")]
    public async Task<IActionResult> SaveMutation(string name, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        switch (name)
        {
            case "createUser":
                response = await accessServiceClient.PostAsync("/api/v1/users", payload, cancellationToken);
                break;

            case "updateUser":
                if (!TryGetGuid(payload, "userId", out var updateUserId))
                {
                    return BadRequest("El payload de 'updateUser' requiere 'userId'.");
                }

                response = await accessServiceClient.PutAsync($"/api/v1/users/{updateUserId}", payload, cancellationToken);
                break;

            case "deactivateUser":
                if (!TryGetGuid(payload, "userId", out var deactivateUserId))
                {
                    return BadRequest("El payload de 'deactivateUser' requiere 'userId'.");
                }

                response = await accessServiceClient.PostAsync($"/api/v1/users/{deactivateUserId}/deactivate", body: null, cancellationToken);
                break;

            case "assignRole":
                if (!TryGetGuid(payload, "userId", out var assignRoleUserId))
                {
                    return BadRequest("El payload de 'assignRole' requiere 'userId'.");
                }

                response = await accessServiceClient.PostAsync($"/api/v1/users/{assignRoleUserId}/role-assignment", payload, cancellationToken);
                break;

            case "createCatalogEntry":
                response = await platformConfigServiceClient.PostAsync("/api/v1/catalogs", payload, cancellationToken);
                break;

            case "updateCatalogEntry":
                if (!TryGetGuid(payload, "entryId", out var updateEntryId))
                {
                    return BadRequest("El payload de 'updateCatalogEntry' requiere 'entryId'.");
                }

                response = await platformConfigServiceClient.PutAsync($"/api/v1/catalogs/{updateEntryId}", payload, cancellationToken);
                break;

            case "deactivateCatalogEntry":
                if (!TryGetGuid(payload, "entryId", out var deactivateEntryId))
                {
                    return BadRequest("El payload de 'deactivateCatalogEntry' requiere 'entryId'.");
                }

                response = await platformConfigServiceClient.PostAsync($"/api/v1/catalogs/{deactivateEntryId}/deactivate", body: null, cancellationToken);
                break;

            case "publishCatalogVersion":
                if (!TryGetString(payload, "catalogType", out var publishCatalogType))
                {
                    return BadRequest("El payload de 'publishCatalogVersion' requiere 'catalogType'.");
                }

                response = await platformConfigServiceClient.PostAsync($"/api/v1/catalogs/{publishCatalogType}/publish", body: null, cancellationToken);
                break;

            case "createCompany":
                response = await partyServiceClient.PostAsync("/api/v1/companies", payload, cancellationToken);
                break;

            case "updateCompany":
                if (!TryGetGuid(payload, "partyId", out var updateCompanyId))
                {
                    return BadRequest("El payload de 'updateCompany' requiere 'partyId'.");
                }

                response = await partyServiceClient.PutAsync($"/api/v1/companies/{updateCompanyId}", payload, cancellationToken);
                break;

            case "createContact":
                var createContactPayload = NormalizePartyPayload(payload);
                response = await partyServiceClient.PostAsync("/api/v1/contacts", createContactPayload, cancellationToken);
                break;

            case "updateContact":
                if (!TryGetGuid(payload, "partyId", out var updateContactId))
                {
                    return BadRequest("El payload de 'updateContact' requiere 'partyId'.");
                }

                var updateContactPayload = NormalizePartyPayload(payload);
                response = await partyServiceClient.PutAsync($"/api/v1/contacts/{updateContactId}", updateContactPayload, cancellationToken);
                break;

            case "relateContactToCompany":
                if (!TryGetGuid(payload, "contactId", out var relateContactId))
                {
                    return BadRequest("El payload de 'relateContactToCompany' requiere 'contactId'.");
                }

                response = await partyServiceClient.PostAsync($"/api/v1/contacts/{relateContactId}/relationships", payload, cancellationToken);
                break;

            case "changePartyCommercialStatus":
                if (!TryGetGuid(payload, "partyId", out var statusPartyId))
                {
                    return BadRequest("El payload de 'changePartyCommercialStatus' requiere 'partyId'.");
                }

                response = await partyServiceClient.PostAsync($"/api/v1/parties/{statusPartyId}/commercial-status", payload, cancellationToken);
                break;

            case "assignPartyResponsible":
                if (!TryGetGuid(payload, "partyId", out var responsiblePartyId))
                {
                    return BadRequest("El payload de 'assignPartyResponsible' requiere 'partyId'.");
                }

                response = await partyServiceClient.PostAsync($"/api/v1/parties/{responsiblePartyId}/responsible", payload, cancellationToken);
                break;
                
            case "createProperty":
                response = await propertyServiceClient.PostAsync("/api/v1/properties", payload, cancellationToken);
                break;

            case "updateProperty":
                if (!TryGetString(payload, "propertyId", out var updatePropertyId)) return BadRequest("Requiere 'propertyId'.");
                response = await propertyServiceClient.PutAsync($"/api/v1/properties/{updatePropertyId}", payload, cancellationToken);
                break;

            case "addPropertyInterest":
                if (!TryGetString(payload, "propertyId", out var interestPropertyId)) return BadRequest("Requiere 'propertyId'.");
                response = await propertyServiceClient.PostAsync($"/api/v1/properties/{interestPropertyId}/interests", payload, cancellationToken);
                break;

            case "createListing":
                response = await supplyServiceClient.PostAsync("/api/v1/listings", payload, cancellationToken);
                break;

            case "updateListing":
                if (!TryGetString(payload, "listingId", out var updateListingId)) return BadRequest("Requiere 'listingId'.");
                response = await supplyServiceClient.PutAsync($"/api/v1/listings/{updateListingId}", payload, cancellationToken);
                break;

            case "activateListing":
                if (!TryGetString(payload, "listingId", out var activateListingId)) return BadRequest("Requiere 'listingId'.");
                response = await supplyServiceClient.PostAsync($"/api/v1/listings/{activateListingId}/activate", payload, cancellationToken);
                break;

            case "pauseListing":
                if (!TryGetString(payload, "listingId", out var pauseListingId)) return BadRequest("Requiere 'listingId'.");
                response = await supplyServiceClient.PostAsync($"/api/v1/listings/{pauseListingId}/pause", payload, cancellationToken);
                break;

            case "closeListing":
                if (!TryGetString(payload, "listingId", out var closeListingId)) return BadRequest("Requiere 'listingId'.");
                response = await supplyServiceClient.PostAsync($"/api/v1/listings/{closeListingId}/close", payload, cancellationToken);
                break;

            case "createRequirement":
                response = await demandServiceClient.PostAsync("/api/v1/requirements", payload, cancellationToken);
                break;

            case "selectListing":
                if (!TryGetString(payload, "requirementId", out var selectRequirementId)) return BadRequest("Requiere 'requirementId'.");
                response = await demandServiceClient.PostAsync($"/api/v1/requirements/{selectRequirementId}/selected-listings", payload, cancellationToken);
                break;

            case "recordVisit":
                response = await commercialServiceClient.PostAsync("/api/visits", payload, cancellationToken);
                break;

            case "recordActivity":
                response = await activityServiceClient.PostAsync("/api/v1/activities", payload, cancellationToken);
                break;

            case "analyzeOpportunity":
                if (!TryGetString(payload, "pipelineItemId", out var pipelineItemId)) return BadRequest("Requiere 'pipelineItemId'.");
                response = await automationAiServiceClient.PostAsync($"/api/v1/ai/opportunities/{pipelineItemId}/analyze", payload, cancellationToken);
                break;

            case "reviewAiDecision":
                if (!TryGetString(payload, "decisionId", out var decisionId)) return BadRequest("Requiere 'decisionId'.");
                response = await automationAiServiceClient.PostAsync($"/api/v1/ai/decisions/{decisionId}/review", payload, cancellationToken);
                break;

            default:
                return NotFound();
        }

        return await ProxyResults.FromAsync(response, cancellationToken);
    }

    private static bool TryGetGuid(JsonElement payload, string propertyName, out Guid value)
    {
        value = Guid.Empty;

        return payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty(propertyName, out var element)
            && element.TryGetGuid(out value);
    }

    private static bool TryGetString(JsonElement payload, string propertyName, out string value)
    {
        value = string.Empty;

        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString() ?? string.Empty;
        return !string.IsNullOrEmpty(value);
    }

    private static object NormalizePartyPayload(JsonElement payload)
    {
        string displayName = string.Empty;
        if (payload.ValueKind == JsonValueKind.Object)
        {
            if (payload.TryGetProperty("displayName", out var dn) && dn.ValueKind == JsonValueKind.String)
                displayName = dn.GetString() ?? string.Empty;
            else if (payload.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                displayName = n.GetString() ?? string.Empty;
        }

        string? phone = TryGetPropertyString(payload, "phone");
        string? email = TryGetPropertyString(payload, "email");
        string? notes = TryGetPropertyString(payload, "notes");

        if (string.IsNullOrWhiteSpace(email)) email = null;
        if (string.IsNullOrWhiteSpace(phone)) phone = null;

        return new
        {
            displayName,
            phone,
            email,
            notes
        };
    }

    private static string? TryGetPropertyString(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var val = prop.GetString();
            return string.IsNullOrWhiteSpace(val) ? null : val;
        }
        return null;
    }
}
