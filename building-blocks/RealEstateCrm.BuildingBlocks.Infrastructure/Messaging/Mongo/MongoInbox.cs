using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Messaging;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;

/// <summary>
/// Adapter Mongo de <see cref="IInbox"/>. La idempotencia (MSG-001) se garantiza con un
/// índice único sobre (EventId, ConsumerName): la segunda inserción del mismo par choca con
/// el índice y <see cref="TryMarkConsumedAsync"/> devuelve <see langword="false"/>.
/// </summary>
/// <remarks>
/// Registrado como singleton: no depende de la sesión/transacción de negocio (el chequeo de
/// idempotencia ocurre del lado del consumidor, no del productor). La creación del índice es
/// perezosa y thread-safe (<see cref="Lazy{T}"/> con <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>,
/// ver skill dotnet-thread-safety-and-shared-state) para no bloquear el arranque del proceso.
/// </remarks>
public sealed class MongoInbox : IInbox
{
    public const string CollectionName = "inbox_consumed_messages";
    private const int DuplicateKeyErrorCode = 11000;

    private readonly IMongoCollection<InboxConsumedMessage> _collection;
    private readonly Lazy<Task> _ensureIndexes;
    private readonly TimeProvider _timeProvider;

    public MongoInbox(IMongoDatabase database, TimeProvider? timeProvider = null)
    {
        _collection = database.GetCollection<InboxConsumedMessage>(CollectionName);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _ensureIndexes = new Lazy<Task>(
            () => CreateIndexesAsync(_collection),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<bool> TryMarkConsumedAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default)
    {
        await _ensureIndexes.Value;

        var record = new InboxConsumedMessage(eventId, consumerName, _timeProvider.GetUtcNow());

        try
        {
            await _collection.InsertOneAsync(record, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    private static async Task CreateIndexesAsync(IMongoCollection<InboxConsumedMessage> collection)
    {
        var keys = Builders<InboxConsumedMessage>.IndexKeys
            .Ascending(m => m.EventId)
            .Ascending(m => m.ConsumerName);

        var model = new CreateIndexModel<InboxConsumedMessage>(
            keys,
            new CreateIndexOptions { Unique = true, Name = "ux_event_consumer" });

        await collection.Indexes.CreateOneAsync(model);
    }
}
