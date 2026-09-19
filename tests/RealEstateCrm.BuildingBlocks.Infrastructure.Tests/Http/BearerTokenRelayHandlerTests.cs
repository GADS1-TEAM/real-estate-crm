using System.Net;
using Microsoft.AspNetCore.Http;
using RealEstateCrm.BuildingBlocks.Infrastructure.Http;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Http;

/// <summary>
/// El relay del Bearer que permite a party-service consultar <c>GET /api/v1/catalogs</c> (que
/// exige JWT en platform-config-service) de forma compartida por los adapters HTTP internos. Sin infraestructura.
/// </summary>
public class BearerTokenRelayHandlerTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static async Task<string?> SendThroughAsync(string? incomingAuthorization, Action<HttpRequestMessage>? configure = null)
    {
        var httpContext = new DefaultHttpContext();
        if (incomingAuthorization is not null)
        {
            httpContext.Request.Headers.Authorization = incomingAuthorization;
        }

        var inner = new CapturingHandler();
        var handler = new BearerTokenRelayHandler(new HttpContextAccessor { HttpContext = httpContext }) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform-config-service.local") };

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/catalogs/CommercialOrigin");
        configure?.Invoke(request);
        await client.SendAsync(request);

        return inner.Authorization;
    }

    [Fact]
    public async Task Forwards_the_bearer_of_the_incoming_request()
    {
        Assert.Equal("Bearer abc.def.ghi", await SendThroughAsync("Bearer abc.def.ghi"));
    }

    [Fact]
    public async Task Sends_no_credentials_when_the_incoming_request_has_none()
    {
        Assert.Null(await SendThroughAsync(incomingAuthorization: null));
    }

    [Fact]
    public async Task Does_not_throw_outside_of_a_request()
    {
        var inner = new CapturingHandler();
        var handler = new BearerTokenRelayHandler(new HttpContextAccessor { HttpContext = null }) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform-config-service.local") };

        await client.GetAsync("/api/v1/catalogs/CommercialOrigin");

        Assert.Null(inner.Authorization);
    }

    [Fact]
    public async Task Does_not_overwrite_an_authorization_already_set_on_the_outgoing_request()
    {
        var sent = await SendThroughAsync("Bearer del-usuario", request =>
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "explicito"));

        Assert.Equal("Bearer explicito", sent);
    }
}
