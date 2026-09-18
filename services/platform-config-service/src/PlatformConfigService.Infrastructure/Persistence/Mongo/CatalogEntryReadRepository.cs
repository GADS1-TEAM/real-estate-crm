using MongoDB.Driver;
using PlatformConfigService.Application.Ports;
using PlatformConfigService.Domain;

namespace PlatformConfigService.Infrastructure.Persistence.Mongo;

/// <summary>
/// Implementación Mongo de <see cref="ICatalogEntryReadPort"/>: consulta directa a
/// <c>IMongoCollection</c> (mongodb-dotnet-driver skill: <c>IRepository</c> no cubre búsquedas
/// distintas de por <c>_id</c> ni listados filtrados).
/// </summary>
public sealed class CatalogEntryReadRepository(IMongoDatabase database) : ICatalogEntryReadPort
{
    private readonly IMongoCollection<CatalogEntry> _collection = database.GetCollection<CatalogEntry>("catalog_entries");

    public async Task<CatalogEntry?> GetByCodeAsync(string catalogType, string code, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CatalogEntry>.Filter.And(
            Builders<CatalogEntry>.Filter.Eq(e => e.CatalogType, catalogType),
            Builders<CatalogEntry>.Filter.Eq(e => e.Code, code));

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogEntry>> ListByCatalogTypeAsync(string catalogType, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var filter = activeOnly
            ? Builders<CatalogEntry>.Filter.And(
                Builders<CatalogEntry>.Filter.Eq(e => e.CatalogType, catalogType),
                Builders<CatalogEntry>.Filter.Eq(e => e.Active, true))
            : Builders<CatalogEntry>.Filter.Eq(e => e.CatalogType, catalogType);

        return await _collection.Find(filter)
            .SortBy(e => e.Order)
            .ThenBy(e => e.Label)
            .ToListAsync(cancellationToken);
    }
}
