using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformConfigService.Application.Catalogs;
using PlatformConfigService.Application.Ports;
using PlatformConfigService.Domain;
using PlatformConfigService.Infrastructure.Persistence.Mongo;
using PlatformConfigService.Infrastructure.Seeding;
using RealEstateCrm.BuildingBlocks.Persistence;

namespace PlatformConfigService.Infrastructure;

/// <summary>Wire-up de platform-config-service: repositorios Mongo y el application service.</summary>
public static class PlatformConfigServiceInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformConfigServiceInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRepository<CatalogEntry, Guid>, CatalogEntryRepository>();
        services.AddScoped<ICatalogEntryReadPort, CatalogEntryReadRepository>();
        services.AddScoped<ICatalogVersionPort, CatalogVersionRepository>();

        services.AddScoped<CatalogService>();

        services.AddHostedService<PlatformConfigServiceIndexInitializer>();

        return services;
    }

    /// <summary>Solo <c>Development</c> (D9): ver <see cref="PlatformConfigServiceDevSeedHostedService"/>.</summary>
    public static IServiceCollection AddPlatformConfigServiceDevSeed(this IServiceCollection services)
    {
        services.AddHostedService<PlatformConfigServiceDevSeedHostedService>();

        return services;
    }
}
