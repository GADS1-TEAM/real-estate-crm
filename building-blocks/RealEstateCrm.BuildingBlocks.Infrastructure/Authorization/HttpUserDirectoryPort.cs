using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Serialization;
using RealEstateCrm.Contracts.Users;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authorization;

/// <summary>
/// Adapter HTTP de <see cref="IUserDirectoryPort"/>: llama a <c>GET /api/v1/users/me</c> en
/// access-service reenviando el Bearer de la request actual (token relay, D6) y cacheando el
/// resultado por <c>sub</c> (mismo <see cref="AuthorizationClientOptions.CacheDuration"/> ≤60 s que
/// <see cref="HttpAuthorizationPort"/>).
/// </summary>
/// <remarks>
/// El cache guarda tanto el 200 como el 403 (<c>user_pending</c>/<c>user_inactive</c>): ambos son
/// una decisión sobre el usuario. Implica la misma ventana de hasta 60 s de retraso ya documentada
/// para las decisiones de autorización. Sin token en la request actual no hay usuario que
/// resolver: falla con <see cref="InvalidOperationException"/> en vez de llamar sin credenciales.
/// </remarks>
public sealed class HttpUserDirectoryPort(
    HttpClient httpClient,
    IMemoryCache cache,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AuthorizationClientOptions> options) : IUserDirectoryPort
{
    private readonly AuthorizationClientOptions _options = options.Value;

    public async Task<UserSelfResult> GetSelfAsync(Guid actorId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user-self:{actorId}";

        if (cache.TryGetValue<UserSelfResult>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorization) || !AuthenticationHeaderValue.TryParse(authorization, out _))
        {
            throw new InvalidOperationException("No hay un Bearer token en la request actual para resolver el usuario (GET /api/v1/users/me).");
        }

        // El Bearer lo adjunta BearerTokenRelayHandler; acá solo se exige que exista.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");

        using var response = await httpClient.SendAsync(request, cancellationToken);

        UserSelfResult result;

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsV1>(RealEstateCrmJsonDefaults.Options, cancellationToken);

            if (string.IsNullOrEmpty(problem?.Detail))
            {
                throw new InvalidOperationException("access-service devolvió 403 sin motivo para GET /api/v1/users/me.");
            }

            result = UserSelfResult.Denied(problem.Detail);
        }
        else
        {
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<UserSelfV1>(RealEstateCrmJsonDefaults.Options, cancellationToken)
                ?? throw new InvalidOperationException("access-service devolvió un body vacío para GET /api/v1/users/me.");

            result = UserSelfResult.Active(user);
        }

        cache.Set(cacheKey, result, _options.CacheDuration);

        return result;
    }
}
