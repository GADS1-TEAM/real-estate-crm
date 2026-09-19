using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Http;

/// <summary>
/// Reenvía el <c>Authorization: Bearer</c> de la request entrante en las llamadas salientes del
/// cliente al que se adjunta (token relay, D6): único mecanismo de relay de los adapters HTTP
/// internos (catálogos, directorio de usuarios). Solo agrega el header si hay un Bearer entrante;
/// sin request en curso (o sin header) no lanza y la llamada sale sin credenciales. No pisa un
/// <c>Authorization</c> ya presente. Registrar como transient y con <c>AddHttpContextAccessor()</c>
/// (ver <see cref="BearerTokenRelayServiceCollectionExtensions"/>).
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

/// <summary>Registro del token relay para clientes HTTP internos.</summary>
public static class BearerTokenRelayServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="BearerTokenRelayHandler"/> (transient) e <c>IHttpContextAccessor</c>, y lo
    /// adjunta al cliente tipado. Idempotente.
    /// </summary>
    public static IHttpClientBuilder AddBearerTokenRelay(this IHttpClientBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddTransient<BearerTokenRelayHandler>();
        return builder.AddHttpMessageHandler<BearerTokenRelayHandler>();
    }
}
