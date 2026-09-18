using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstateCrm.BuildingBlocks.Authorization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authorization;

/// <summary>
/// Registro de <see cref="HttpUserDirectoryPort"/>. Aditivo respecto de
/// <c>AddAuthorizationHttpClients</c>: reutiliza la misma sección de configuración
/// (<c>AccessService:BaseUrl</c>/<c>CacheDuration</c>) y se llama a continuación de aquel.
/// </summary>
public static class UserDirectoryClientServiceCollectionExtensions
{
    public static IServiceCollection AddUserDirectoryHttpClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "AccessService")
    {
        services.Configure<AuthorizationClientOptions>(configuration.GetSection(configurationSectionName));
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        var baseUrl = configuration.GetSection(configurationSectionName)["BaseUrl"]
            ?? throw new InvalidOperationException($"Falta configuración '{configurationSectionName}:BaseUrl' (URL de access-service).");

        services.AddHttpClient<IUserDirectoryPort, HttpUserDirectoryPort>(client =>
            client.BaseAddress = new Uri(baseUrl));

        return services;
    }
}
