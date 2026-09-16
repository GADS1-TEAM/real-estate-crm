using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstateCrm.BuildingBlocks.Messaging;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Registro de mensajería RabbitMQ compartida: conexión del proceso, <see cref="IEventPublisher"/>
/// y el host de consumo (<see cref="RabbitMqEventConsumerHost"/>) que cada servicio usa para
/// suscribir sus <see cref="IEventConsumer{TPayload}"/>.
/// </summary>
public static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "RabbitMq")
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(configurationSectionName));

        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddSingleton<RabbitMqEventConsumerHost>();

        return services;
    }
}
