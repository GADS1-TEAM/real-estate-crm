using PlatformConfigService.Application.Ports;
using PlatformConfigService.Domain;
using RealEstateCrm.TestSupport.Persistence;

namespace PlatformConfigService.Application.Tests.Fakes;

/// <summary>
/// Implementación en memoria de <see cref="ICatalogEntryReadPort"/> para tests de Application.
/// Lee del mismo <see cref="InMemoryRepository{TAggregate, TId}"/> que usa el test como
/// <c>IRepository{CatalogEntry, Guid}</c> (mismo criterio que <c>FakeUserAccountReadPort</c>,
/// V2-ACL-001).
/// </summary>
public sealed class FakeCatalogEntryReadPort(InMemoryRepository<CatalogEntry, Guid> repository) : ICatalogEntryReadPort
{
    public Task<CatalogEntry?> GetByCodeAsync(string catalogType, string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(repository.All.FirstOrDefault(e => e.CatalogType == catalogType && e.Code == code));

    public Task<IReadOnlyList<CatalogEntry>> ListByCatalogTypeAsync(string catalogType, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = repository.All.Where(e => e.CatalogType == catalogType);

        if (activeOnly)
        {
            query = query.Where(e => e.Active);
        }

        var ordered = query.OrderBy(e => e.Order ?? int.MaxValue).ThenBy(e => e.Label, StringComparer.Ordinal).ToList();

        return Task.FromResult<IReadOnlyList<CatalogEntry>>(ordered);
    }
}
