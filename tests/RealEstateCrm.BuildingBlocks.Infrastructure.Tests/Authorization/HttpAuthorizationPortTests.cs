using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Infrastructure.Authorization;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Authorization;

/// <summary>
/// D2: caché de <see cref="HttpAuthorizationPort"/> corta (≤60 s). No necesita access-service
/// real: un <see cref="HttpMessageHandler"/> que cuenta invocaciones alcanza para probar que la
/// segunda llamada con la misma clave no vuelve a pegarle a la red.
/// </summary>
public class HttpAuthorizationPortTests
{
    private sealed class CountingHandler(Func<HttpRequestMessage, AuthorizationDecision> respond) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var decision = respond(request);
            var json = JsonSerializer.Serialize(decision, RealEstateCrmJsonDefaults.Options);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    private static HttpAuthorizationPort NewPort(CountingHandler handler, TimeSpan cacheDuration)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://access-service.local") };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new AuthorizationClientOptions { CacheDuration = cacheDuration });
        return new HttpAuthorizationPort(httpClient, cache, options);
    }

    [Fact]
    public async Task Second_call_with_the_same_key_is_served_from_cache()
    {
        var handler = new CountingHandler(_ => AuthorizationDecision.Allow());
        var port = NewPort(handler, TimeSpan.FromMinutes(1));
        var actorId = Guid.NewGuid();

        var first = await port.EvaluateAsync(actorId, Permissions.PartiesRead, ResourceTypes.Party, null);
        var second = await port.EvaluateAsync(actorId, Permissions.PartiesRead, ResourceTypes.Party, null);

        Assert.True(first.Allowed);
        Assert.True(second.Allowed);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task A_different_permission_is_not_served_from_the_other_permissions_cache_entry()
    {
        var handler = new CountingHandler(_ => AuthorizationDecision.Allow());
        var port = NewPort(handler, TimeSpan.FromMinutes(1));
        var actorId = Guid.NewGuid();

        await port.EvaluateAsync(actorId, Permissions.PartiesRead, ResourceTypes.Party, null);
        await port.EvaluateAsync(actorId, Permissions.PartiesWrite, ResourceTypes.Party, null);

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task After_the_cache_duration_expires_the_decision_is_evaluated_again()
    {
        var handler = new CountingHandler(_ => AuthorizationDecision.Allow());
        var port = NewPort(handler, TimeSpan.FromMilliseconds(50));
        var actorId = Guid.NewGuid();

        await port.EvaluateAsync(actorId, Permissions.PartiesRead, ResourceTypes.Party, null);
        await Task.Delay(TimeSpan.FromMilliseconds(150));
        await port.EvaluateAsync(actorId, Permissions.PartiesRead, ResourceTypes.Party, null);

        Assert.Equal(2, handler.CallCount);
    }
}
