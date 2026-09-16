using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Messaging.Mongo;

/// <summary>
/// MSG-001: un segundo consumo del mismo eventId no repite la proyección. Necesita un Mongo
/// real (standalone alcanza, no requiere replica set: la idempotencia depende del índice
/// único, no de una transacción).
/// </summary>
/// <remarks>
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresMongo"</c>.
/// Para correrlo: <c>docker run --rm -p 27017:27017 mongo:7</c>.
/// </remarks>
[Trait("Category", "RequiresMongo")]
public class MongoInboxIdempotencyTests : IAsyncLifetime
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017";
    private const string DatabaseNamePrefix = "fnd002_inbox_test_";

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
    public async Task Second_consumption_of_the_same_event_id_is_skipped()
    {
        var inbox = new MongoInbox(_database);
        var eventId = Guid.NewGuid();

        var firstAttempt = await inbox.TryMarkConsumedAsync(eventId, "party-projection-consumer");
        var secondAttempt = await inbox.TryMarkConsumedAsync(eventId, "party-projection-consumer");

        Assert.True(firstAttempt, "El primer consumo de un eventId nuevo debe ejecutar el efecto.");
        Assert.False(secondAttempt, "El segundo consumo del mismo eventId no debe repetir el efecto.");
    }

    [Fact]
    public async Task Same_event_id_for_a_different_consumer_is_not_blocked()
    {
        var inbox = new MongoInbox(_database);
        var eventId = Guid.NewGuid();

        var firstConsumer = await inbox.TryMarkConsumedAsync(eventId, "party-projection-consumer");
        var secondConsumer = await inbox.TryMarkConsumedAsync(eventId, "analytics-projection-consumer");

        Assert.True(firstConsumer);
        Assert.True(secondConsumer, "Cada consumer procesa el evento por separado, un mismo eventId puede tener varios consumidores.");
    }
}
