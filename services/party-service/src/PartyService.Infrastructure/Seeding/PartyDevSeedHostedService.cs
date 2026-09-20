using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RealEstateCrm.BuildingBlocks.Persistence;
using PartyService.Domain;

namespace PartyService.Infrastructure.Seeding;

public sealed class PartyDevSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PartyDevSeedHostedService> _logger;
    private readonly IHostEnvironment _env;

    public PartyDevSeedHostedService(
        IServiceProvider serviceProvider,
        ILogger<PartyDevSeedHostedService> logger,
        IHostEnvironment env)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _env = env;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Party, Guid>>();

        await SeedPartyAsync(repository, new Guid("d0000000-0000-0000-0000-000000000001"), PartyKind.NaturalPerson, "Carlos Propietario", CommercialStatus.Customer, cancellationToken);
        await SeedPartyAsync(repository, new Guid("d0000000-0000-0000-0000-000000000002"), PartyKind.LegalEntity, "Inmuebles del Sur SA", CommercialStatus.Customer, cancellationToken);
        await SeedPartyAsync(repository, new Guid("d0000000-0000-0000-0000-000000000003"), PartyKind.NaturalPerson, "María Interesada", CommercialStatus.Potential, cancellationToken);
    }

    private async Task SeedPartyAsync(IRepository<Party, Guid> repository, Guid id, PartyKind kind, string displayName, CommercialStatus commercialStatus, CancellationToken ct)
    {
        var existing = await repository.GetByIdAsync(id, ct);
        if (existing is not null)
        {
            _logger.LogInformation("Party {Id} already exists, skipping seed", id);
            return;
        }

        var party = new Party(
            id,
            kind,
            new PartyProfile(displayName, null, null, null, null, null, null, null, null, null, null),
            IdentityStatus.Active,
            commercialStatus,
            new Guid("a0000000-0000-0000-0000-000000000001"), // access-service admin user
            null,
            null,
            DateTimeOffset.UtcNow,
            new Guid("a0000000-0000-0000-0000-000000000001"),
            DateTimeOffset.UtcNow,
            new Guid("a0000000-0000-0000-0000-000000000001"),
            DateTimeOffset.UtcNow,
            new Guid("a0000000-0000-0000-0000-000000000001"),
            1);

        await repository.AddAsync(party, ct);
        _logger.LogInformation("Seeded Party {Id} ({DisplayName})", id, displayName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
