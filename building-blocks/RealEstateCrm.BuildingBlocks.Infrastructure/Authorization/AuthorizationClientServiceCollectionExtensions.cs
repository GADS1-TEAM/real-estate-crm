using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstateCrm.BuildingBlocks.Authorization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Authorization;

/// <summary>
/// Registro del cliente HTTP interno hacia access-service: <see cref="HttpAuthorizationPort"/> y
/// <see cref="HttpResponsibleAssignmentValidationPort"/>, ambos con <c>IMemoryCache</c> corta (D2).
/// </summary>
/// <remarks>
/// Lo usan los servicios owner que consumen access-service por HTTP (ej. <c>party-service</c>,
/// V2-PTY-001). <c>access-service</c> mismo no lo registra (ver <see cref="HttpAuthorizationPort"/>).
/// </remarks>
public static class AuthorizationClientServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationHttpClients(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "AccessService")
    {
        services.Configure<AuthorizationClientOptions>(configuration.GetSection(configurationSectionName));
        services.AddMemoryCache();

        var baseUrl = configuration.GetSection(configurationSectionName)["BaseUrl"]
            ?? throw new InvalidOperationException($"Falta configuración '{configurationSectionName}:BaseUrl' (URL de access-service).");

        services.AddHttpClient<IAuthorizationPort, HttpAuthorizationPort>(client =>
            client.BaseAddress = new Uri(baseUrl));

        services.AddHttpClient<IResponsibleAssignmentValidationPort, HttpResponsibleAssignmentValidationPort>(client =>
            client.BaseAddress = new Uri(baseUrl));

        return services;
    }
}
