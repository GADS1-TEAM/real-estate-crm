using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Declara la topología de <see cref="RabbitMqConsumerRegistration"/> (exchange, cola, DLQ) y
/// conecta un <see cref="IEventConsumer{TPayload}"/> a ella, resolviendo idempotencia vía
/// <see cref="IInbox"/> antes de invocar al consumer.
/// </summary>
public sealed class RabbitMqEventConsumerHost
{
    private readonly RabbitMqConnectionProvider _connectionProvider;

    public RabbitMqEventConsumerHost(RabbitMqConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    /// <summary>
    /// Declara exchange/cola/DLQ y arranca el consumo. El <see cref="IChannel"/> devuelto queda
    /// vivo mientras no se disponga: quien llama es responsable de mantenerlo (ej. un
    /// <c>BackgroundService</c> del Api) y de disponerlo al apagar el proceso.
    /// </summary>
    public async Task<IChannel> StartAsync<TPayload>(
        RabbitMqConsumerRegistration registration,
        IEventConsumer<TPayload> consumer,
        IInbox inbox,
        CancellationToken cancellationToken = default)
    {
        var channel = await _connectionProvider.CreateChannelAsync(cancellationToken);

        await channel.ExchangeDeclareAsync(
            registration.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            registration.ResolvedDeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] = registration.ResolvedDeadLetterQueueName,
        };

        await channel.QueueDeclareAsync(
            registration.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            registration.QueueName,
            registration.ExchangeName,
            registration.RoutingKey,
            cancellationToken: cancellationToken);

        var consumerTag = new AsyncEventingBasicConsumer(channel);
        consumerTag.ReceivedAsync += async (_, delivery) =>
            await HandleDeliveryAsync(channel, delivery, consumer, inbox, cancellationToken);

        await channel.BasicConsumeAsync(
            registration.QueueName,
            autoAck: false,
            consumer: consumerTag,
            cancellationToken: cancellationToken);

        return channel;
    }

    private static async Task HandleDeliveryAsync<TPayload>(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        IEventConsumer<TPayload> consumer,
        IInbox inbox,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = Encoding.UTF8.GetString(delivery.Body.Span);
            var envelope = JsonSerializer.Deserialize<EventEnvelopeV1<TPayload>>(json, RealEstateCrmJsonDefaults.Options)
                ?? throw new JsonException("El body del mensaje no deserializa a un EventEnvelopeV1 válido.");

            var isFirstDelivery = await inbox.TryMarkConsumedAsync(envelope.EventId, consumer.ConsumerName, cancellationToken);

            if (isFirstDelivery)
            {
                await consumer.HandleAsync(envelope, cancellationToken);
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
        }
    }
}
