using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RealEstateCrm.BuildingBlocks.Authentication;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authentication;

/// <summary>
/// Adapter de <see cref="IAuthenticationPort"/> que resuelve el usuario a partir del
/// <see cref="ClaimsPrincipal"/> ya autenticado por el middleware JwtBearer (token emitido
/// por Keycloak) para la request HTTP actual.
/// </summary>
/// <remarks>
/// No valida el token: eso ya lo hizo JwtBearer antes de que el pipeline llegue acá. Este
/// adapter solo mapea claims del token a <see cref="AuthenticatedUser"/>.
/// </remarks>
public sealed class ClaimsPrincipalAuthenticationPort : IAuthenticationPort
{
    private const string RealmAccessClaimType = "realm_access";
    private const string SubjectClaimType = "sub";
    private const string EmailClaimType = "email";
    private const string NameClaimType = "name";
    private const string PreferredUsernameClaimType = "preferred_username";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsPrincipalAuthenticationPort(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<AuthenticatedUser?> ResolveCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        var subject = principal.FindFirstValue(SubjectClaimType)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (subject is null || !Guid.TryParse(subject, out var userId))
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        var displayName = principal.FindFirstValue(NameClaimType)
            ?? principal.FindFirstValue(PreferredUsernameClaimType)
            ?? string.Empty;

        var email = principal.FindFirstValue(EmailClaimType) ?? string.Empty;

        var roles = ExtractRealmRoles(principal.FindFirstValue(RealmAccessClaimType));

        return Task.FromResult<AuthenticatedUser?>(new AuthenticatedUser(userId, displayName, email, roles));
    }

    /// <summary>
    /// Keycloak emite los roles de realm como el claim JSON <c>realm_access: { "roles": [...] }</c>,
    /// no como claims individuales. Este método extrae esa lista.
    /// </summary>
    public static IReadOnlyCollection<string> ExtractRealmRoles(string? realmAccessClaimJson)
    {
        if (string.IsNullOrWhiteSpace(realmAccessClaimJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            using var document = JsonDocument.Parse(realmAccessClaimJson);

            if (!document.RootElement.TryGetProperty("roles", out var rolesElement) ||
                rolesElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            return rolesElement
                .EnumerateArray()
                .Select(role => role.GetString())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role!)
                .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
