using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Readiness check de MongoDB: hace <c>ping</c> contra la base "admin" con el
/// <see cref="IMongoClient"/> ya registrado por <c>AddMongoPersistence</c>. No valida
/// ownership de colecciones ni datos, solo que el servidor responde.
/// </summary>
public sealed class MongoHealthCheck(IMongoClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetDatabase("admin")
                .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("No se pudo hacer ping a MongoDB.", ex);
        }
    }
}
