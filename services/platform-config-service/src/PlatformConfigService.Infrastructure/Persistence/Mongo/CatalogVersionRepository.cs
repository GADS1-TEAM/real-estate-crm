using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlatformConfigService.Application.Ports;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

namespace PlatformConfigService.Infrastructure.Persistence.Mongo;

/// <summary>
/// Documento de la colección <c>catalog_type_versions</c> (D8): un contador por
/// <c>catalogType</c>, <c>_id = catalogType</c>. Solo existe en Infrastructure (sin equivalente
/// en Domain: no es un aggregate de negocio con invariantes, es el contador de publicación
/// decidido para <c>PublishCatalogVersion</c>, ver <see cref="ICatalogVersionPort"/>).
/// </summary>
internal sealed record CatalogTypeVersionDocument(
    [property: BsonId] string CatalogType,
    int CurrentVersion);

/// <summary>
/// Implementación Mongo de <see cref="ICatalogVersionPort"/>. <see cref="IncrementAsync"/> usa
/// <c>FindOneAndUpdate</c> con <c>$inc</c> (atómico a nivel de un solo documento) y respeta la
/// sesión/transacción ambiente (<see cref="MongoSessionAccessor"/>) para quedar atómico junto con
/// el evento de outbox encolado en la misma operación (mismo mecanismo que
/// <c>VersionedMongoRepository</c>).
/// </summary>
public sealed class CatalogVersionRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor) : ICatalogVersionPort
{
    private readonly IMongoCollection<CatalogTypeVersionDocument> _collection =
        database.GetCollection<CatalogTypeVersionDocument>("catalog_type_versions");

    public async Task<int> GetCurrentAsync(string catalogType, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CatalogTypeVersionDocument>.Filter.Eq(d => d.CatalogType, catalogType);

        var cursor = sessionAccessor.CurrentSession is { } session
            ? await _collection.FindAsync(session, filter, cancellationToken: cancellationToken)
            : await _collection.FindAsync(filter, cancellationToken: cancellationToken);

        var document = await cursor.FirstOrDefaultAsync(cancellationToken);

        return document?.CurrentVersion ?? 0;
    }

    public async Task<int> IncrementAsync(string catalogType, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CatalogTypeVersionDocument>.Filter.Eq(d => d.CatalogType, catalogType);
        var update = Builders<CatalogTypeVersionDocument>.Update.Inc(d => d.CurrentVersion, 1);
        var options = new FindOneAndUpdateOptions<CatalogTypeVersionDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
        };

        var result = sessionAccessor.CurrentSession is { } session
            ? await _collection.FindOneAndUpdateAsync(session, filter, update, options, cancellationToken)
            : await _collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);

        return result.CurrentVersion;
    }
}
