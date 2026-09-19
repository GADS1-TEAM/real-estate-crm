using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Infrastructure.Authorization;
using RealEstateCrm.BuildingBlocks.Infrastructure.Http;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Serialization;
using RealEstateCrm.Contracts.Users;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Authorization;

/// <summary>
/// <see cref="HttpUserDirectoryPort"/> sin access-service real: un handler que cuenta
/// invocaciones y registra el header enviado prueba el token relay, el cache por <c>sub</c> y el
/// tratamiento de 403/5xx.
/// </summary>
public class HttpUserDirectoryPortTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public string? LastAuthorization { get; private set; }

        public string? LastPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastAuthorization = request.Headers.Authorization?.ToString();
            LastPath = request.RequestUri?.PathAndQuery;
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object body) =>
        new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, RealEstateCrmJsonDefaults.Options), Encoding.UTF8, "application/json"),
        };

    private static HttpUserDirectoryPort NewPort(StubHandler handler, string? authorizationHeader = "Bearer token-del-usuario", TimeSpan? cacheDuration = null)
    {
        var httpContext = new DefaultHttpContext();
        if (authorizationHeader is not null)
        {
            httpContext.Request.Headers.Authorization = authorizationHeader;
        }

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        // Misma cadena que AddUserDirectoryHttpClient: el relay adjunta el Bearer, el port solo lo exige.
        var relay = new BearerTokenRelayHandler(accessor) { InnerHandler = handler };

        return new HttpUserDirectoryPort(
            new HttpClient(relay) { BaseAddress = new Uri("http://access-service.local") },
            new MemoryCache(new MemoryCacheOptions()),
            accessor,
            Options.Create(new AuthorizationClientOptions { CacheDuration = cacheDuration ?? TimeSpan.FromMinutes(1) }));
    }

    private static ProblemDetailsV1 Forbidden(string reason) =>
        new("about:blank", "Acción prohibida.", 403, reason, "/api/v1/users/me", ErrorCodes.Forbidden, Guid.NewGuid());

    [Fact]
    public async Task Resolves_the_user_and_relays_the_bearer_token()
    {
        var self = new UserSelfV1(Guid.NewGuid(), "Active", "Vendedor");
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, self));
        var port = NewPort(handler);

        var result = await port.GetSelfAsync(Guid.NewGuid());

        Assert.True(result.IsActive);
        Assert.Equal(self, result.User);
        Assert.Equal("Bearer token-del-usuario", handler.LastAuthorization);
        Assert.Equal("/api/v1/users/me", handler.LastPath);
    }

    [Theory]
    [InlineData(DenyReasons.UserPending)]
    [InlineData(DenyReasons.UserInactive)]
    public async Task A_403_becomes_a_denied_result_with_the_access_service_reason_code(string reason)
    {
        var port = NewPort(new StubHandler(_ => Json(HttpStatusCode.Forbidden, Forbidden(reason))));

        var result = await port.GetSelfAsync(Guid.NewGuid());

        Assert.False(result.IsActive);
        Assert.Equal(reason, result.DenyReasonCode);
    }

    [Fact]
    public async Task The_result_is_cached_per_sub_for_the_configured_duration()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, new UserSelfV1(Guid.NewGuid(), "Active", "Vendedor")));
        var port = NewPort(handler);
        var sub = Guid.NewGuid();

        var first = await port.GetSelfAsync(sub);
        var second = await port.GetSelfAsync(sub);
        await port.GetSelfAsync(Guid.NewGuid());

        Assert.Equal(first, second);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task A_denial_is_also_cached()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.Forbidden, Forbidden(DenyReasons.UserInactive)));
        var port = NewPort(handler);
        var sub = Guid.NewGuid();

        await port.GetSelfAsync(sub);
        await port.GetSelfAsync(sub);

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task An_expired_cache_entry_calls_access_service_again()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, new UserSelfV1(Guid.NewGuid(), "Active", null)));
        var port = NewPort(handler, cacheDuration: TimeSpan.FromMilliseconds(50));
        var sub = Guid.NewGuid();

        await port.GetSelfAsync(sub);
        await Task.Delay(150);
        await port.GetSelfAsync(sub);

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task Without_a_bearer_token_it_fails_instead_of_calling_without_credentials()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, new UserSelfV1(Guid.NewGuid(), "Active", null)));
        var port = NewPort(handler, authorizationHeader: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => port.GetSelfAsync(Guid.NewGuid()));
        Assert.Equal(0, handler.CallCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Other_failures_surface_as_http_errors_and_are_not_cached(HttpStatusCode status)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status));
        var port = NewPort(handler);
        var sub = Guid.NewGuid();

        await Assert.ThrowsAsync<HttpRequestException>(() => port.GetSelfAsync(sub));
        await Assert.ThrowsAsync<HttpRequestException>(() => port.GetSelfAsync(sub));

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task A_malformed_or_empty_body_is_an_error_not_a_default_user()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") });
        var port = NewPort(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => port.GetSelfAsync(Guid.NewGuid()));
    }
}
