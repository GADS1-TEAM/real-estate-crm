using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Events;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Persistence.Mongo;

/// <summary>
/// Decisión del equipo (V2-FND-002): el outbox usa una transacción multi-documento para
/// quedar atómico con el aggregate. Necesita un Mongo con replica set (a cargo de V2-FND-003);
/// en un Mongo standalone <c>StartTransaction</c> falla y este test lo demuestra.
/// </summary>
/// <remarks>
/// Mismo Trait que <see cref="Mongo.MongoInboxIdempotencyTests"/> (<c>RequiresMongo</c>), pero
/// contra una instancia Mongo *distinta*: esta necesita replica set, esa alcanza con standalone.
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresMongo"</c>.
/// Para correrlo con un replica set de un solo nodo (puerto separado del standalone de
/// <see cref="Mongo.MongoInboxIdempotencyTests"/>, configurable con
/// <c>MONGO_REPLICA_SET_CONNECTION_STRING</c>):
/// <c>docker run --rm -p 27018:27018 mongo:7 mongod --replSet rs0 --port 27018 --bind_ip_all</c>,
/// después <c>docker exec &lt;container&gt; mongosh --port 27018 --eval
/// 'rs.initiate({_id:"rs0", members:[{_id:0, host:"localhost:27018"}]})'</c>.
/// </remarks>
[Trait("Category", "RequiresMongo")]
public class MongoOutboxTransactionTests : IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("MONGO_REPLICA_SET_CONNECTION_STRING")
        ?? "mongodb://localhost:27018/?replicaSet=rs0";

    private const string DatabaseNamePrefix = "fnd002_outbox_test_";

    private sealed record SampleAggregate([property: BsonId] Guid Id, string Name);

    private sealed class SampleRepository : MongoRepository<SampleAggregate, Guid>
    {
        public SampleRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
            : base(database, "sample_aggregates", sessionAccessor, aggregate => aggregate.Id)
        {
        }
    }

    private IMongoClient _client = null!;
    private IMongoDatabase _database = null!;
    private string _databaseName = null!;

    public Task InitializeAsync()
    {
        _client = new MongoClient(ConnectionString);
        _databaseName = DatabaseNamePrefix + Guid.NewGuid().ToString("N");
        _database = _client.GetDatabase(_databaseName);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

    [Fact]
    public async Task Aggregate_write_and_outbox_enqueue_commit_atomically()
    {
        var sessionAccessor = new MongoSessionAccessor();
        var repository = new SampleRepository(_database, sessionAccessor);
        var outbox = new MongoOutbox(_database, sessionAccessor);
        var unitOfWork = new MongoUnitOfWork(_client, sessionAccessor);

        var aggregate = new SampleAggregate(Guid.NewGuid(), "Empresa Demo SA");
        var envelope = new EventEnvelopeV1<object>(
            Guid.NewGuid(), "PartyRegistered", 1, DateTimeOffset.UtcNow,
            Guid.NewGuid(), Guid.NewGuid(), null, aggregate.Id, new { aggregate.Name });

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await repository.AddAsync(aggregate, ct);
            await outbox.EnqueueAsync(OutboxMessage.From(envelope, DateTimeOffset.UtcNow), ct);
        });

        var persistedAggregate = await repository.GetByIdAsync(aggregate.Id);
        var pendingMessages = await outbox.GetPendingAsync(batchSize: 10);

        Assert.NotNull(persistedAggregate);
        Assert.Equal("Empresa Demo SA", persistedAggregate!.Name);
        Assert.Contains(pendingMessages, m => m.EventId == envelope.EventId);
    }

    [Fact]
    public async Task Aborted_transaction_leaves_neither_aggregate_nor_outbox_message()
    {
        var sessionAccessor = new MongoSessionAccessor();
        var repository = new SampleRepository(_database, sessionAccessor);
        var outbox = new MongoOutbox(_database, sessionAccessor);
        var unitOfWork = new MongoUnitOfWork(_client, sessionAccessor);

        var aggregate = new SampleAggregate(Guid.NewGuid(), "Empresa Que No Debe Quedar");
        var envelope = new EventEnvelopeV1<object>(
            Guid.NewGuid(), "PartyRegistered", 1, DateTimeOffset.UtcNow,
            Guid.NewGuid(), Guid.NewGuid(), null, aggregate.Id, new { aggregate.Name });

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await repository.AddAsync(aggregate, ct);
            await outbox.EnqueueAsync(OutboxMessage.From(envelope, DateTimeOffset.UtcNow), ct);
            throw new InvalidOperationException("simulated failure after both writes");
        }));

        var persistedAggregate = await repository.GetByIdAsync(aggregate.Id);
        var pendingMessages = await outbox.GetPendingAsync(batchSize: 10);

        Assert.Null(persistedAggregate);
        Assert.DoesNotContain(pendingMessages, m => m.EventId == envelope.EventId);
    }
}
