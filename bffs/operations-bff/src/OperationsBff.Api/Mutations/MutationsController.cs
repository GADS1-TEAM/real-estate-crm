using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OperationsBff.Api.AccessService;

namespace OperationsBff.Api.Mutations;

/// <summary>
/// <c>POST /mutations/{name}</c> (D5). Implementa las 4 mutaciones de administración de
/// usuarios de V2-ACL-001: <c>createUser</c>, <c>updateUser</c>, <c>deactivateUser</c>,
/// <c>assignRole</c>. El body se reenvía tal cual a access-service (que ignora campos extra,
/// ej. <c>userId</c> en el body de <c>updateUser</c>/<c>assignRole</c>, ya usado para armar la ruta).
/// </summary>
[ApiController]
[Route("mutations")]
[Authorize]
public sealed class MutationsController(AccessServiceClient accessServiceClient) : ControllerBase
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
                if (!TryGetUserId(payload, out var updateUserId))
                {
                    return BadRequest("El payload de 'updateUser' requiere 'userId'.");
                }

                response = await accessServiceClient.PutAsync($"/api/v1/users/{updateUserId}", payload, cancellationToken);
                break;

            case "deactivateUser":
                if (!TryGetUserId(payload, out var deactivateUserId))
                {
                    return BadRequest("El payload de 'deactivateUser' requiere 'userId'.");
                }

                response = await accessServiceClient.PostAsync($"/api/v1/users/{deactivateUserId}/deactivate", body: null, cancellationToken);
                break;

            case "assignRole":
                if (!TryGetUserId(payload, out var assignRoleUserId))
                {
                    return BadRequest("El payload de 'assignRole' requiere 'userId'.");
                }

                response = await accessServiceClient.PostAsync($"/api/v1/users/{assignRoleUserId}/role-assignment", payload, cancellationToken);
                break;

            default:
                return NotFound();
        }

        return await ProxyResults.FromAsync(response, cancellationToken);
    }

    private static bool TryGetUserId(JsonElement payload, out Guid userId)
    {
        userId = Guid.Empty;

        return payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty("userId", out var userIdElement)
            && userIdElement.TryGetGuid(out userId);
    }
}
