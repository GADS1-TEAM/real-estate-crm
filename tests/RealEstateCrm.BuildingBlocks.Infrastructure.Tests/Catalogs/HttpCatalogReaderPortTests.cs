using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Infrastructure.Catalogs;
using RealEstateCrm.Contracts.Catalogs;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Catalogs;

/// <summary>
/// D7: caché de <see cref="HttpCatalogReaderPort"/> corta (≤60 s). No necesita
/// platform-config-service real: un <see cref="HttpMessageHandler"/> que cuenta invocaciones
/// alcanza para probar que la segunda llamada con la misma clave no vuelve a pegarle a la red
/// (mismo patrón que <c>HttpAuthorizationPortTests</c>, V2-ACL-001).
/// </summary>
public class HttpCatalogReaderPortTests
{
    private sealed class CountingHandler(Func<HttpRequestMessage, CatalogQueryResultV1> respond) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var result = respond(request);
            var json = JsonSerializer.Serialize(result, RealEstateCrmJsonDefaults.Options);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    private static HttpCatalogReaderPort NewPort(CountingHandler handler, TimeSpan cacheDuration)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://platform-config-service.local") };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CatalogClientOptions { CacheDuration = cacheDuration });
        return new HttpCatalogReaderPort(httpClient, cache, options);
    }

    private static CatalogQueryResultV1 EmptyResultAtVersion(int version) => new(Array.Empty<CatalogEntryV1>(), version);

    [Fact]
    public async Task Second_call_with_the_same_key_is_served_from_cache()
    {
        var handler = new CountingHandler(_ => EmptyResultAtVersion(1));
        var port = NewPort(handler, TimeSpan.FromMinutes(1));

        var first = await port.GetAsync(CatalogTypes.LossReason, activeOnly: true);
        var second = await port.GetAsync(CatalogTypes.LossReason, activeOnly: true);

        Assert.Equal(1, first.CatalogVersion);
        Assert.Equal(1, second.CatalogVersion);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task A_different_activeOnly_value_is_not_served_from_the_other_cache_entry()
    {
        var handler = new CountingHandler(_ => EmptyResultAtVersion(1));
        var port = NewPort(handler, TimeSpan.FromMinutes(1));

        await port.GetAsync(CatalogTypes.LossReason, activeOnly: true);
        await port.GetAsync(CatalogTypes.LossReason, activeOnly: false);

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task A_different_catalogType_is_not_served_from_the_other_cache_entry()
    {
        var handler = new CountingHandler(_ => EmptyResultAtVersion(1));
        var port = NewPort(handler, TimeSpan.FromMinutes(1));

        await port.GetAsync(CatalogTypes.LossReason, activeOnly: true);
        await port.GetAsync(CatalogTypes.CommercialOrigin, activeOnly: true);

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task After_the_cache_duration_expires_the_catalog_is_fetched_again()
    {
        var handler = new CountingHandler(_ => EmptyResultAtVersion(1));
        var port = NewPort(handler, TimeSpan.FromMilliseconds(50));

        await port.GetAsync(CatalogTypes.LossReason, activeOnly: true);
        await Task.Delay(TimeSpan.FromMilliseconds(150));
        await port.GetAsync(CatalogTypes.LossReason, activeOnly: true);

        Assert.Equal(2, handler.CallCount);
    }
}
