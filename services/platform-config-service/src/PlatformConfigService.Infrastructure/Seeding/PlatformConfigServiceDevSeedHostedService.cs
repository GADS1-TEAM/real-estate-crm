using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlatformConfigService.Application.Ports;
using PlatformConfigService.Domain;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Catalogs;

namespace PlatformConfigService.Infrastructure.Seeding;

/// <summary>
/// Seed idempotente de desarrollo (D9, CAT-006): los seis catálogos con los defaults literales de
/// <see cref="DevSeedCatalogEntries"/>, y una publicación inicial (versión 1) de cada
/// <c>catalogType</c>. Solo se registra en <c>Development</c> (ver <c>Program.cs</c>).
/// </summary>
/// <remarks>
/// No pasa por <c>CatalogService</c> ni emite eventos de outbox (mismo criterio que
/// <c>AccessServiceDevSeedHostedService</c>, V2-ACL-001): es carga de datos de bootstrap, no un
/// caso de uso de negocio.
/// <para>
/// <see cref="IHostedService"/> se registra siempre como singleton, pero sus dependencias
/// (repositorios, puertos) son Scoped: por eso resuelve un <see cref="IServiceScope"/> propio en
/// <see cref="StartAsync"/> en vez de inyectarlas directo en el constructor (mismo motivo que
/// <c>OutboxRelayBackgroundService</c>/<c>AccessServiceDevSeedHostedService</c>).
/// </para>
/// </remarks>
public sealed class PlatformConfigServiceDevSeedHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<PlatformConfigServiceDevSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var catalogEntryReads = scope.ServiceProvider.GetRequiredService<ICatalogEntryReadPort>();
        var catalogEntries = scope.ServiceProvider.GetRequiredService<IRepository<CatalogEntry, Guid>>();
        var catalogVersions = scope.ServiceProvider.GetRequiredService<ICatalogVersionPort>();

        var seededAny = false;

        foreach (var seed in DevSeedCatalogEntries.All)
        {
            var existing = await catalogEntryReads.GetByCodeAsync(seed.CatalogType, seed.Code, cancellationToken);

            if (existing is not null)
            {
                continue;
            }

            var entry = CatalogEntry.Create(seed.CatalogType, seed.Code, seed.Label, seed.Order, seed.PipelineKind, seed.SemanticState);
            await catalogEntries.AddAsync(entry, cancellationToken);
            seededAny = true;
        }

        if (seededAny)
        {
            foreach (var catalogType in CatalogTypes.All)
            {
                var currentVersion = await catalogVersions.GetCurrentAsync(catalogType, cancellationToken);

                if (currentVersion == 0)
                {
                    await catalogVersions.IncrementAsync(catalogType, cancellationToken);
                }
            }

            logger.LogInformation(
                "Seed de desarrollo: {Count} entradas de catálogo sembradas en {CatalogTypeCount} catalogTypes.",
                DevSeedCatalogEntries.All.Count,
                CatalogTypes.All.Count);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
