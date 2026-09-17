using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Outbox;

/// <summary>Registro del relay reutilizable de Outbox (ver <see cref="OutboxRelayBackgroundService"/>).</summary>
public static class OutboxRelayServiceCollectionExtensions
{
    /// <summary>
    /// Agrega el <see cref="OutboxRelayBackgroundService"/> como <see cref="IHostedService"/>.
    /// Requiere que <c>AddMongoPersistence</c> (o cualquier otro adapter de <c>IOutbox</c>) y
    /// <c>AddRabbitMqMessaging</c> (o cualquier otro adapter de <c>IEventPublisher</c>) ya estén
    /// registrados.
    /// </summary>
    public static IServiceCollection AddOutboxRelay(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "OutboxRelay")
    {
        services.Configure<OutboxRelayOptions>(configuration.GetSection(configurationSectionName));
        services.AddHostedService<OutboxRelayBackgroundService>();

        return services;
    }
}
