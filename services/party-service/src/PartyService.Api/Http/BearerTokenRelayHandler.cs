using System.Net.Http.Headers;

namespace PartyService.Api.Http;

/// <summary>
/// Reenvía el <c>Authorization: Bearer</c> de la request entrante en las llamadas salientes del
/// cliente al que se adjunta (token relay, D6). Se usa para <c>GET /api/v1/catalogs/...</c>, que
/// exige JWT en platform-config-service. No pisa un <c>Authorization</c> ya presente.
/// </summary>
public sealed class BearerTokenRelayHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null
            && httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString() is { Length: > 0 } authorization
            && AuthenticationHeaderValue.TryParse(authorization, out var header))
        {
            request.Headers.Authorization = header;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
