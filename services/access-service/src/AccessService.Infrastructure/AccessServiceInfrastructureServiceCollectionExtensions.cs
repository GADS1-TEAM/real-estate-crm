using AccessService.Application.Authorization;
using AccessService.Application.Ports;
using AccessService.Application.Users;
using AccessService.Domain;
using AccessService.Infrastructure.Persistence.Mongo;
using AccessService.Infrastructure.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.BuildingBlocks.Persistence;

namespace AccessService.Infrastructure;

/// <summary>Wire-up de access-service: repositorios Mongo, puertos de autorización locales y el application service.</summary>
public static class AccessServiceInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAccessServiceInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRepository<UserAccount, Guid>, UserAccountRepository>();
        services.AddScoped<IRepository<RoleAssignment, Guid>, RoleAssignmentRepository>();
        services.AddScoped<IUserAccountReadPort, UserAccountReadRepository>();

        // Evaluador local: access-service NO usa HttpAuthorizationPort para sí mismo (ver
        // AccessServiceAuthorizationEvaluator).
        services.AddScoped<IAuthorizationPort, AccessServiceAuthorizationEvaluator>();
        services.AddScoped<IResponsibleAssignmentValidationPort, AccessServiceResponsibleAssignmentValidator>();

        services.AddScoped<UserAccountService>();
        services.AddScoped<UserSelfService>();

        services.AddHostedService<AccessServiceIndexInitializer>();

        return services;
    }

    /// <summary>Solo <c>Development</c> (D9): ver <see cref="AccessServiceDevSeedHostedService"/>.</summary>
    public static IServiceCollection AddAccessServiceDevSeed(this IServiceCollection services)
    {
        services.AddHostedService<AccessServiceDevSeedHostedService>();

        return services;
    }
}
