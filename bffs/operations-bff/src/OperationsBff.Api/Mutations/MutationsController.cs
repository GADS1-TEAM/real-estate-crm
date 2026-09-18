using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OperationsBff.Api.AccessService;
using OperationsBff.Api.PlatformConfigService;

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
[Authorize]
public sealed class MutationsController(
    AccessServiceClient accessServiceClient,
    PlatformConfigServiceClient platformConfigServiceClient) : ControllerBase
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
}
