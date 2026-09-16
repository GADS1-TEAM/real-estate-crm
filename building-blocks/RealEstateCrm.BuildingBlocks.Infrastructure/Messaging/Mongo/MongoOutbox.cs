using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;
using RealEstateCrm.BuildingBlocks.Messaging;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;

/// <summary>
/// Adapter Mongo de <see cref="IOutbox"/>. <see cref="EnqueueAsync"/> participa en la
/// transacción ambiente de <see cref="MongoSessionAccessor"/> para quedar atómico con el
/// cambio de negocio; <see cref="GetPendingAsync"/>/<see cref="MarkPublishedAsync"/> los usa
/// el proceso que drena el outbox, fuera de cualquier transacción de negocio.
/// </summary>
public sealed class MongoOutbox : IOutbox
{
    public const string CollectionName = "outbox_messages";

    private readonly IMongoCollection<OutboxMessage> _collection;
    private readonly MongoSessionAccessor _sessionAccessor;

    public MongoOutbox(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
    {
        _collection = database.GetCollection<OutboxMessage>(CollectionName);
        _sessionAccessor = sessionAccessor;
    }

    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
        _sessionAccessor.CurrentSession is { } session
            ? _collection.InsertOneAsync(session, message, cancellationToken: cancellationToken)
            : _collection.InsertOneAsync(message, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<OutboxMessage>.Filter.Eq(m => m.PublishedAt, null);

        return await _collection
            .Find(filter)
            .SortBy(m => m.CreatedAt)
            .Limit(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task MarkPublishedAsync(Guid eventId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        var filter = Builders<OutboxMessage>.Filter.Eq(m => m.EventId, eventId);
        var update = Builders<OutboxMessage>.Update.Set(m => m.PublishedAt, publishedAt);

        return _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}
