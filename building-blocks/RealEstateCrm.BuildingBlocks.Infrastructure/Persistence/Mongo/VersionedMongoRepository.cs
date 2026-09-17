using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Persistence;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

/// <summary>
/// Base genérica de <see cref="IRepository{TAggregate, TId}"/> sobre una colección Mongo, con
/// concurrencia optimista por un campo de versión entero. Cada servicio la extiende con su
/// aggregate, su colección propia, su selector de id y su selector de versión.
/// </summary>
/// <remarks>
/// La versión se lee por delegado (<paramref name="versionSelector"/> del constructor), no por
/// una interfaz que el aggregate deba implementar: los aggregates viven en la capa Domain de
/// cada servicio, que no puede referenciar ningún proyecto (pureza de dominio, ver test de
/// arquitectura <c>Domain_projects_do_not_reference_infrastructure_frameworks_or_other_projects</c>
/// en <c>RealEstateCrm.ArchitectureTests</c>).
/// <para>
/// Convención de concurrencia optimista: cada método de dominio que muta el aggregate incrementa
/// su versión en 1 antes de que la Application layer llame a <see cref="UpdateAsync"/>. Este
/// adapter filtra la actualización por <c>id + (versión actual del aggregate - 1)</c> — la
/// versión que tenía antes de la mutación en memoria —; si cero documentos matchean, alguien más
/// ya lo actualizó primero y se lanza <see cref="ConcurrencyConflictException"/> en vez de
/// sobrescribir en silencio.
/// </para>
/// <para>
/// No reemplaza a <see cref="MongoRepository{TAggregate, TId}"/> (que no versiona): es una
/// alternativa para aggregates con campo de versión. Misma API pública que
/// <see cref="IRepository{TAggregate, TId}"/>, sin parámetros nuevos en los métodos: no rompe
/// ningún llamador existente de <c>IRepository</c>. Tampoco aplica ningún filtro de
/// tenant/organización (ADR-001).
/// </para>
/// </remarks>
public abstract class VersionedMongoRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : class
{
    private readonly IMongoCollection<TAggregate> _collection;
    private readonly MongoSessionAccessor _sessionAccessor;
    private readonly string _idFieldName;
    private readonly string _versionFieldName;
    private readonly Func<TAggregate, TId> _idSelector;
    private readonly Func<TAggregate, int> _versionSelector;

    protected VersionedMongoRepository(
        IMongoDatabase database,
        string collectionName,
        MongoSessionAccessor sessionAccessor,
        Func<TAggregate, TId> idSelector,
        Func<TAggregate, int> versionSelector,
        string idFieldName = "_id",
        string versionFieldName = "Version")
    {
        _collection = database.GetCollection<TAggregate>(collectionName);
        _sessionAccessor = sessionAccessor;
        _idSelector = idSelector;
        _versionSelector = versionSelector;
        _idFieldName = idFieldName;
        _versionFieldName = versionFieldName;
    }

    public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TAggregate>.Filter.Eq(_idFieldName, id);

        var cursor = _sessionAccessor.CurrentSession is { } session
            ? await _collection.FindAsync(session, filter, cancellationToken: cancellationToken)
            : await _collection.FindAsync(filter, cancellationToken: cancellationToken);

        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default) =>
        _sessionAccessor.CurrentSession is { } session
            ? _collection.InsertOneAsync(session, aggregate, cancellationToken: cancellationToken)
            : _collection.InsertOneAsync(aggregate, cancellationToken: cancellationToken);

    /// <summary>
    /// Reemplaza el documento filtrando por <c>id</c> y por la versión ANTERIOR a la mutación en
    /// memoria. Si cero documentos matchean, lanza <see cref="ConcurrencyConflictException"/>.
    /// </summary>
    public async Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var id = _idSelector(aggregate);
        var expectedPreviousVersion = _versionSelector(aggregate) - 1;

        var filter = Builders<TAggregate>.Filter.And(
            Builders<TAggregate>.Filter.Eq(_idFieldName, id),
            Builders<TAggregate>.Filter.Eq(_versionFieldName, expectedPreviousVersion));

        var result = _sessionAccessor.CurrentSession is { } session
            ? await _collection.ReplaceOneAsync(session, filter, aggregate, cancellationToken: cancellationToken)
            : await _collection.ReplaceOneAsync(filter, aggregate, cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyConflictException(typeof(TAggregate).Name, id!);
        }
    }
}
