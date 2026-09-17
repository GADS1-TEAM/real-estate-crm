using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authorization;

/// <summary>
/// Adapter HTTP de <see cref="IAuthorizationPort"/>: llama a
/// <c>POST /api/v1/authorization/evaluate</c> en access-service, con caché corta en memoria (D2).
/// </summary>
/// <remarks>
/// Lo usan los servicios owner (ej. <c>party-service</c> en V2-PTY-001) para preguntarle a
/// access-service. <c>access-service</c> NO se registra a sí mismo con este adapter: evalúa la
/// matriz localmente (ver <c>AccessServiceAuthorizationEvaluator</c> en
/// <c>AccessService.Application</c>) para no hacer un loop HTTP contra sí mismo.
/// </remarks>
public sealed class HttpAuthorizationPort(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<AuthorizationClientOptions> options) : IAuthorizationPort
{
    private readonly AuthorizationClientOptions _options = options.Value;

    public async Task<AuthorizationDecision> EvaluateAsync(
        Guid actorId,
        string permission,
        string resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"authz:{actorId}:{permission}:{resourceType}:{resourceId}";

        if (cache.TryGetValue<AuthorizationDecision>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var request = new AuthorizationEvaluationRequestV1(actorId, permission, resourceType, resourceId);

        using var response = await httpClient.PostAsJsonAsync(
            "/api/v1/authorization/evaluate",
            request,
            RealEstateCrmJsonDefaults.Options,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var decision = await response.Content.ReadFromJsonAsync<AuthorizationDecision>(
            RealEstateCrmJsonDefaults.Options,
            cancellationToken)
            ?? throw new InvalidOperationException("access-service devolvió un body vacío para /api/v1/authorization/evaluate.");

        cache.Set(cacheKey, decision, _options.CacheDuration);

        return decision;
    }
}
