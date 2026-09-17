using AccessService.Application.Ports;
using AccessService.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealEstateCrm.BuildingBlocks.Persistence;

namespace AccessService.Infrastructure.Seeding;

/// <summary>
/// Seed idempotente de desarrollo (D9): 1 Administrador, 1 Vendedor y 1 Responsable Comercial,
/// vinculados a los usuarios fijos del realm-export. Solo se registra en <c>Development</c> (ver
/// <c>Program.cs</c>).
/// </summary>
/// <remarks>
/// No pasa por <c>UserAccountService</c> (que requiere un actor ya existente para
/// <c>AssignRoleAsync</c> — problema del huevo y la gallina en el primer arranque) ni emite
/// eventos de outbox: es carga de datos de bootstrap, no un caso de uso de negocio.
/// <para>
/// <see cref="IHostedService"/> se registra siempre como singleton, pero sus dependencias
/// (<see cref="IUserAccountReadPort"/>, los repositorios) son Scoped (atadas a
/// <c>MongoSessionAccessor</c>, ver V2-FND-002): por eso resuelve un <see cref="IServiceScope"/>
/// propio en <see cref="StartAsync"/> en vez de inyectarlas directo en el constructor (mismo
/// motivo que <c>OutboxRelayBackgroundService</c>).
/// </para>
/// </remarks>
public sealed class AccessServiceDevSeedHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AccessServiceDevSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var userAccountReads = scope.ServiceProvider.GetRequiredService<IUserAccountReadPort>();
        var userAccounts = scope.ServiceProvider.GetRequiredService<IRepository<UserAccount, Guid>>();
        var roleAssignments = scope.ServiceProvider.GetRequiredService<IRepository<RoleAssignment, Guid>>();

        foreach (var seed in DevSeedUsers.All)
        {
            var existing = await userAccountReads.GetByKeycloakSubjectAsync(seed.KeycloakSubject, cancellationToken);

            if (existing is not null)
            {
                continue;
            }

            var user = UserAccount.CreateActive(seed.KeycloakSubject, seed.DisplayName, seed.Email);
            await userAccounts.AddAsync(user, cancellationToken);

            var assignment = RoleAssignment.Create(user.UserId, seed.RoleCode, assignedByUserId: user.UserId, timeProvider.GetUtcNow());
            await roleAssignments.AddAsync(assignment, cancellationToken);

            logger.LogInformation(
                "Seed de desarrollo: usuario {DisplayName} creado con rol {RoleCode}.",
                seed.DisplayName,
                seed.RoleCode);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
