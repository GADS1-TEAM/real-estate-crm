using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstateCrm.BuildingBlocks.Catalogs;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Catalogs;

/// <summary>
/// Registro del cliente HTTP interno hacia <c>platform-config-service</c>: <see cref="HttpCatalogReaderPort"/>
/// con <c>IMemoryCache</c> corta (D7/D2). Lo usan los servicios owner que consumen catálogos por
/// HTTP (ej. <c>party-service</c>, V2-PTY-001); <c>platform-config-service</c> mismo no lo
/// registra (ver <see cref="HttpCatalogReaderPort"/>).
/// </summary>
public static class CatalogClientServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogHttpClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "PlatformConfigService")
    {
        services.Configure<CatalogClientOptions>(configuration.GetSection(configurationSectionName));
        services.AddMemoryCache();

        var baseUrl = configuration.GetSection(configurationSectionName)["BaseUrl"]
            ?? throw new InvalidOperationException($"Falta configuración '{configurationSectionName}:BaseUrl' (URL de platform-config-service).");

        services.AddHttpClient<ICatalogReaderPort, HttpCatalogReaderPort>(client =>
            client.BaseAddress = new Uri(baseUrl));

        return services;
    }
}
