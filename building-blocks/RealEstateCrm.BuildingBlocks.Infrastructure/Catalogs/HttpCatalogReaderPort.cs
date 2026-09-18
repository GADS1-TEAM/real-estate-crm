using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Catalogs;
using RealEstateCrm.Contracts.Catalogs;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Catalogs;

/// <summary>
/// Adapter HTTP de <see cref="ICatalogReaderPort"/> (D7): llama a
/// <c>GET /api/v1/catalogs/{catalogType}</c> en <c>platform-config-service</c>, con caché corta
/// en memoria (mismo patrón que <see cref="RealEstateCrm.BuildingBlocks.Infrastructure.Authorization.HttpAuthorizationPort"/>).
/// </summary>
/// <remarks>
/// <c>platform-config-service</c> mismo no se registra con este adapter (evita el loop HTTP
/// contra sí mismo): sirve su propio catálogo vía repositorio Mongo directo.
/// </remarks>
public sealed class HttpCatalogReaderPort(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<CatalogClientOptions> options) : ICatalogReaderPort
{
    private readonly CatalogClientOptions _options = options.Value;

    public async Task<CatalogQueryResultV1> GetAsync(string catalogType, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"catalog:{catalogType}:{activeOnly}";

        if (cache.TryGetValue<CatalogQueryResultV1>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        using var response = await httpClient.GetAsync(
            $"/api/v1/catalogs/{catalogType}?activeOnly={activeOnly}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CatalogQueryResultV1>(
            RealEstateCrmJsonDefaults.Options,
            cancellationToken)
            ?? throw new InvalidOperationException("platform-config-service devolvió un body vacío para GET /api/v1/catalogs/{catalogType}.");

        cache.Set(cacheKey, result, _options.CacheDuration);

        return result;
    }
}
