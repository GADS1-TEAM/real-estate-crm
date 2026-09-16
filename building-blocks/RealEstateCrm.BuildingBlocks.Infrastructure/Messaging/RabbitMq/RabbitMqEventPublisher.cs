using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RealEstateCrm.BuildingBlocks.Messaging;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Adapter de <see cref="IEventPublisher"/> sobre RabbitMQ. Publica en el exchange topic
/// propio del servicio (<see cref="RabbitMqOptions.ServiceExchangeName"/>) con routing key
/// igual al nombre del evento (<see cref="OutboxMessage.Name"/>), mensaje persistente.
/// </summary>
/// <remarks>
/// Lo usa el proceso que drena el <see cref="IOutbox"/>, nunca código de negocio directamente.
/// </remarks>
public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly string _exchangeName;

    public RabbitMqEventPublisher(RabbitMqConnectionProvider connectionProvider, IOptions<RabbitMqOptions> options)
    {
        _connectionProvider = connectionProvider;
        _exchangeName = options.Value.ServiceExchangeName;
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await using var channel = await _connectionProvider.CreateChannelAsync(cancellationToken);

        await channel.ExchangeDeclareAsync(
            _exchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = message.EventId.ToString(),
            Headers = new Dictionary<string, object?>
            {
                ["eventId"] = message.EventId.ToString(),
                ["version"] = message.Version,
                ["actorId"] = message.ActorId.ToString(),
                ["correlationId"] = message.CorrelationId.ToString(),
                ["causationId"] = message.CausationId?.ToString(),
                ["aggregateId"] = message.AggregateId.ToString(),
            },
        };

        var body = Encoding.UTF8.GetBytes(message.EnvelopeJson);

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: message.Name,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
