using MongoDB.Driver;
using PlatformConfigService.Domain;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

namespace PlatformConfigService.Infrastructure.Persistence.Mongo;

/// <summary>Repositorio de <see cref="CatalogEntry"/> sobre la colección <c>catalog_entries</c> (D8), con concurrencia optimista (mismo patrón que <c>UserAccountRepository</c>, V2-ACL-001).</summary>
public sealed class CatalogEntryRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
    : VersionedMongoRepository<CatalogEntry, Guid>(
        database,
        collectionName: "catalog_entries",
        sessionAccessor,
        idSelector: entry => entry.EntryId,
        versionSelector: entry => entry.Version);
