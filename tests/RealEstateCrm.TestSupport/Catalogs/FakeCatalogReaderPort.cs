using RealEstateCrm.BuildingBlocks.Catalogs;
using RealEstateCrm.Contracts.Catalogs;

namespace RealEstateCrm.TestSupport.Catalogs;

/// <summary>
/// Implementación en memoria de <see cref="ICatalogReaderPort"/> para tests de servicios owner
/// que validan un <c>code</c> contra un catálogo (ej. <c>originCode</c> de party-service).
/// </summary>
public sealed class FakeCatalogReaderPort : ICatalogReaderPort
{
    private readonly Dictionary<string, List<CatalogEntryV1>> _entries = new();

    public int CatalogVersion { get; set; } = 1;

    public int CallCount { get; private set; }

    public FakeCatalogReaderPort WithEntry(string catalogType, string code, bool active = true)
    {
        if (!_entries.TryGetValue(catalogType, out var list))
        {
            list = new List<CatalogEntryV1>();
            _entries[catalogType] = list;
        }

        list.Add(new CatalogEntryV1(Guid.NewGuid(), catalogType, code, code, null, null, null, active));
        return this;
    }

    public Task<CatalogQueryResultV1> GetAsync(string catalogType, bool activeOnly, CancellationToken cancellationToken = default)
    {
        CallCount++;

        var entries = _entries.GetValueOrDefault(catalogType) ?? new List<CatalogEntryV1>();
        var filtered = activeOnly ? entries.Where(entry => entry.Active).ToList() : entries;

        return Task.FromResult(new CatalogQueryResultV1(filtered, CatalogVersion));
    }
}
