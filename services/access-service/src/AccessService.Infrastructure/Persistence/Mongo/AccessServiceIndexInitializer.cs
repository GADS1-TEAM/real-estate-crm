using AccessService.Domain;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace AccessService.Infrastructure.Persistence.Mongo;

/// <summary>
/// Crea, de forma idempotente, el índice único de <c>user_accounts.KeycloakSubject</c>
/// (mongodb-document-modeling skill: unicidad declarada por índice, no por validación de
/// aplicación). <see cref="IMongoCollection{TDocument}.Indexes"/>.CreateOne con las mismas
/// keys/opciones no duplica el índice en corridas sucesivas.
/// </summary>
public sealed class AccessServiceIndexInitializer(IMongoDatabase database) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<UserAccount>("user_accounts");

        var keys = Builders<UserAccount>.IndexKeys.Ascending(u => u.KeycloakSubject);
        var options = new CreateIndexOptions { Unique = true, Name = "uq_user_accounts_keycloak_subject" };

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<UserAccount>(keys, options), cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
