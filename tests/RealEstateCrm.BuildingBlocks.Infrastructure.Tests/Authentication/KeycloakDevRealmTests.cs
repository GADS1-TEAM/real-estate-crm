using System.Net.Http.Json;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Authentication;

/// <summary>
/// Prueba end-to-end contra un Keycloak real de desarrollo (criterio de aceptación de
/// V2-FND-002: "Keycloak autentica un usuario de desarrollo"). El realm local con usuario de
/// desarrollo lo crea V2-FND-003 (Compose), que todavía no existe en este repo.
/// </summary>
/// <remarks>
/// Decisión del equipo (V2-FND-002): en vez de mockear Keycloak, este test se excluye por
/// defecto con <c>[Trait("Category", "RequiresKeycloak")]</c> y se corre a mano cuando haya un
/// Keycloak levantado (variables de entorno KEYCLOAK_BASE_URL, KEYCLOAK_REALM,
/// KEYCLOAK_CLIENT_ID, KEYCLOAK_DEV_USERNAME, KEYCLOAK_DEV_PASSWORD). Comando para excluirlo
/// explícitamente (ya excluido por defecto si se usa este filtro):
/// <c>dotnet test --filter "Category!=RequiresKeycloak"</c>.
/// El resto de los tests de autenticación (<see cref="JwtBearerAuthenticationPipelineTests"/>,
/// <see cref="ExtractRealmRolesTests"/>) usan un token firmado localmente o el fake, y sí
/// corren siempre.
/// </remarks>
[Trait("Category", "RequiresKeycloak")]
public class KeycloakDevRealmTests
{
    [Fact]
    public async Task Keycloak_authenticates_a_dev_user_via_password_grant()
    {
        var baseUrl = Environment.GetEnvironmentVariable("KEYCLOAK_BASE_URL") ?? "http://localhost:8080";
        var realm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? "crm-dev";
        var clientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "operations-bff";
        var username = Environment.GetEnvironmentVariable("KEYCLOAK_DEV_USERNAME") ?? "dev.vendedor";
        var password = Environment.GetEnvironmentVariable("KEYCLOAK_DEV_PASSWORD") ?? "dev.vendedor";

        using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

        var response = await client.PostAsync(
            $"/realms/{realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = clientId,
                ["username"] = username,
                ["password"] = password,
            }));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(payload);
        Assert.True(payload.ContainsKey("access_token"), "Keycloak no devolvió access_token.");
    }
}
