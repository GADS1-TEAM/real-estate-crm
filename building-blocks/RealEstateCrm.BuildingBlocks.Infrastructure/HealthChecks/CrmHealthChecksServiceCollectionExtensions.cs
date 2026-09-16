using Microsoft.Extensions.DependencyInjection;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Extensión reutilizable de healthchecks (OPS-003, V2-FND-003) para que cada servicio/BFF
/// registre <c>/health/live</c> y <c>/health/ready</c> sin repetir la configuración.
/// </summary>
/// <remarks>
/// Ningún <c>Program.cs</c> de <c>services/</c> o <c>bffs/</c> invoca esto todavía: registrar
/// health checks ahí queda fuera de la write zone de V2-FND-003 (ver IMPLEMENTATION_REPORT).
/// </remarks>
public static class CrmHealthChecksServiceCollectionExtensions
{
    /// <summary>Punto de entrada del builder de healthchecks del proceso. No registra ningún check por sí solo.</summary>
    public static IHealthChecksBuilder AddCrmHealthChecks(this IServiceCollection services)
        => services.AddHealthChecks();

    /// <summary>
    /// Agrega el readiness check de MongoDB. Requiere que <c>AddMongoPersistence</c> ya haya
    /// registrado un <see cref="MongoDB.Driver.IMongoClient"/>.
    /// </summary>
    public static IHealthChecksBuilder AddMongoReadinessCheck(this IHealthChecksBuilder builder, string name = "mongo")
        => builder.AddCheck<MongoHealthCheck>(name, tags: [CrmHealthCheckTags.Ready]);

    /// <summary>
    /// Agrega el readiness check de RabbitMQ. Requiere que <c>AddRabbitMqMessaging</c> ya haya
    /// registrado un <see cref="Messaging.RabbitMq.RabbitMqConnectionProvider"/>.
    /// </summary>
    public static IHealthChecksBuilder AddRabbitMqReadinessCheck(this IHealthChecksBuilder builder, string name = "rabbitmq")
        => builder.AddCheck<RabbitMqHealthCheck>(name, tags: [CrmHealthCheckTags.Ready]);
}
