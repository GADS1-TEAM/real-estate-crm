using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Persistence;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

/// <summary>
/// Base genérica de <see cref="IRepository{TAggregate, TId}"/> sobre una colección Mongo.
/// Cada servicio la extiende con su aggregate, su colección propia y su selector de id.
/// </summary>
/// <remarks>
/// No aplica ningún filtro de tenant/organización (ADR-001): la query es siempre por id de
/// aggregate. Si una query necesita autorización, el servicio owner la agrega explícitamente
/// en su propio repository, no acá.
/// </remarks>
public abstract class MongoRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : class
{
    private readonly IMongoCollection<TAggregate> _collection;
    private readonly MongoSessionAccessor _sessionAccessor;
    private readonly string _idFieldName;
    private readonly Func<TAggregate, TId> _idSelector;

    protected MongoRepository(
        IMongoDatabase database,
        string collectionName,
        MongoSessionAccessor sessionAccessor,
        Func<TAggregate, TId> idSelector,
        string idFieldName = "_id")
    {
        _collection = database.GetCollection<TAggregate>(collectionName);
        _sessionAccessor = sessionAccessor;
        _idSelector = idSelector;
        _idFieldName = idFieldName;
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

    public Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TAggregate>.Filter.Eq(_idFieldName, _idSelector(aggregate));

        return _sessionAccessor.CurrentSession is { } session
            ? _collection.ReplaceOneAsync(session, filter, aggregate, cancellationToken: cancellationToken)
            : _collection.ReplaceOneAsync(filter, aggregate, cancellationToken: cancellationToken);
    }
}
