using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authorization;

/// <summary>
/// Adapter HTTP de <see cref="IResponsibleAssignmentValidationPort"/>: llama a
/// <c>POST /api/v1/assignments/validate</c> en access-service, con la misma caché corta que
/// <see cref="HttpAuthorizationPort"/> (D2).
/// </summary>
public sealed class HttpResponsibleAssignmentValidationPort(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<AuthorizationClientOptions> options) : IResponsibleAssignmentValidationPort
{
    private readonly AuthorizationClientOptions _options = options.Value;

    public async Task<AuthorizationDecision> ValidateAsync(
        Guid actorId,
        string resourceType,
        Guid resourceId,
        Guid responsibleUserId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"assign-validate:{actorId}:{resourceType}:{resourceId}:{responsibleUserId}";

        if (cache.TryGetValue<AuthorizationDecision>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var request = new ResponsibleAssignmentValidationRequestV1(actorId, resourceType, resourceId, responsibleUserId);

        using var response = await httpClient.PostAsJsonAsync(
            "/api/v1/assignments/validate",
            request,
            RealEstateCrmJsonDefaults.Options,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var decision = await response.Content.ReadFromJsonAsync<AuthorizationDecision>(
            RealEstateCrmJsonDefaults.Options,
            cancellationToken)
            ?? throw new InvalidOperationException("access-service devolvió un body vacío para /api/v1/assignments/validate.");

        cache.Set(cacheKey, decision, _options.CacheDuration);

        return decision;
    }
}
