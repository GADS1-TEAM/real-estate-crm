using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using PlatformConfigService.Domain;

namespace PlatformConfigService.Infrastructure.Persistence.Mongo;

/// <summary>
/// Crea, de forma idempotente, el índice único compuesto <c>catalog_entries.(CatalogType, Code)</c>
/// (mongodb-document-modeling skill: unicidad declarada por índice, no por validación de
/// aplicación; mismo patrón que <c>AccessServiceIndexInitializer</c>, V2-ACL-001).
/// </summary>
public sealed class PlatformConfigServiceIndexInitializer(IMongoDatabase database) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<CatalogEntry>("catalog_entries");

        var keys = Builders<CatalogEntry>.IndexKeys
            .Ascending(e => e.CatalogType)
            .Ascending(e => e.Code);
        var options = new CreateIndexOptions { Unique = true, Name = "uq_catalog_entries_catalog_type_code" };

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<CatalogEntry>(keys, options), cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
