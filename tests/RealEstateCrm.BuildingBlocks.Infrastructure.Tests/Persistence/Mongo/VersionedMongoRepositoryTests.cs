using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;
using RealEstateCrm.BuildingBlocks.Persistence;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Persistence.Mongo;

/// <summary>
/// Concurrencia optimista de <see cref="VersionedMongoRepository{TAggregate, TId}"/> (pedido
/// explícito de V2-ACL-001: reutilizable, no algo casero de access-service). Standalone alcanza:
/// no depende de una transacción, solo del filtro <c>id + versión anterior</c>.
/// </summary>
/// <remarks>
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresMongo"</c>.
/// Para correrlo: <c>docker run --rm -p 27017:27017 mongo:7</c>.
/// </remarks>
[Trait("Category", "RequiresMongo")]
public class VersionedMongoRepositoryTests : IAsyncLifetime
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017";
    private const string DatabaseNamePrefix = "acl001_versioned_repo_test_";

    private sealed record SampleAggregate([property: BsonId] Guid Id, string Name, int Version);

    private sealed class SampleRepository : VersionedMongoRepository<SampleAggregate, Guid>
    {
        public SampleRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
            : base(database, "sample_versioned_aggregates", sessionAccessor, a => a.Id, a => a.Version)
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
    public async Task UpdateAsync_succeeds_when_the_expected_previous_version_matches()
    {
        var repository = new SampleRepository(_database, new MongoSessionAccessor());
        var original = new SampleAggregate(Guid.NewGuid(), "v1", Version: 1);
        await repository.AddAsync(original);

        var updated = original with { Name = "v2", Version = 2 };
        await repository.UpdateAsync(updated);

        var persisted = await repository.GetByIdAsync(original.Id);
        Assert.Equal("v2", persisted!.Name);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task UpdateAsync_throws_ConcurrencyConflictException_when_another_writer_already_advanced_the_version()
    {
        var repository = new SampleRepository(_database, new MongoSessionAccessor());
        var original = new SampleAggregate(Guid.NewGuid(), "v1", Version: 1);
        await repository.AddAsync(original);

        // Otro actor ya avanzó a v2 mientras tanto.
        await repository.UpdateAsync(original with { Name = "v2-por-otro-actor", Version = 2 });

        // Este writer todavía tiene la copia v1 en memoria: intenta ir a v2 pero ya no existe v1 persistida.
        var staleUpdate = original with { Name = "v2-stale", Version = 2 };

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => repository.UpdateAsync(staleUpdate));

        var persisted = await repository.GetByIdAsync(original.Id);
        Assert.Equal("v2-por-otro-actor", persisted!.Name); // el update perdedor no pisó al ganador.
    }
}
