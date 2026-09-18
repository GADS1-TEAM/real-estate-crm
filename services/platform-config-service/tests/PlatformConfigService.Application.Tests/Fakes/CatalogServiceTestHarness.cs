using PlatformConfigService.Application.Catalogs;
using PlatformConfigService.Domain;
using RealEstateCrm.TestSupport.Messaging;
using RealEstateCrm.TestSupport.Persistence;

namespace PlatformConfigService.Application.Tests.Fakes;

/// <summary>Wiring en memoria de <see cref="CatalogService"/> compartido por los tests de Application.</summary>
public sealed class CatalogServiceTestHarness
{
    public InMemoryRepository<CatalogEntry, Guid> CatalogEntries { get; } = new(e => e.EntryId);

    public InMemoryOutbox Outbox { get; } = new();

    public InMemoryCatalogVersionPort CatalogVersions { get; } = new();

    public FakeCatalogEntryReadPort CatalogEntryReads { get; }

    public CatalogService Service { get; }

    public CatalogServiceTestHarness()
    {
        CatalogEntryReads = new FakeCatalogEntryReadPort(CatalogEntries);
        Service = new CatalogService(
            CatalogEntries,
            CatalogEntryReads,
            CatalogVersions,
            new PassthroughUnitOfWork(),
            Outbox,
            TimeProvider.System);
    }
}
