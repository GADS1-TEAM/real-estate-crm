using Microsoft.Extensions.DependencyInjection;
using PartyService.Application.Parties;
using PartyService.Application.Ports;
using PartyService.Domain;
using PartyService.Infrastructure.Persistence.Mongo;
using RealEstateCrm.BuildingBlocks.Persistence;

namespace PartyService.Infrastructure;

/// <summary>Wire-up de party-service: repositorios Mongo, puerto de lectura y application service.</summary>
public static class PartyServiceInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPartyServiceInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRepository<Party, Guid>, PartyRepository>();
        services.AddScoped<IRepository<PartyRelationship, Guid>, PartyRelationshipRepository>();
        services.AddScoped<IPartyReadPort, PartyReadRepository>();

        services.AddScoped<PartyManagementService>();

        services.AddHostedService<PartyServiceIndexInitializer>();

        return services;
    }
}
