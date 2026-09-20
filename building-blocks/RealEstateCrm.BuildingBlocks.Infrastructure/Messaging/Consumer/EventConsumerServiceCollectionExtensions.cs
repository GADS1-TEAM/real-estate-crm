using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;
using RealEstateCrm.Contracts;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Consumer;

public static class EventConsumerServiceCollectionExtensions
{
    public static IServiceCollection AddEventConsumer<TPayload, TConsumer>(
        this IServiceCollection services,
        string exchange,
        string eventName,
        string queueName)
        where TConsumer : class, IEventConsumer<TPayload>
    {
        services.AddScoped<TConsumer>();
        
        services.AddHostedService(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMqEventConsumerHost<TPayload, TConsumer>>>();
            var connectionProvider = sp.GetRequiredService<RabbitMqConnectionProvider>();
            
            return new RabbitMqEventConsumerHost<TPayload, TConsumer>(
                connectionProvider,
                exchange,
                eventName,
                queueName,
                sp,
                logger);
        });

        return services;
    }
}

internal sealed class RabbitMqEventConsumerHost<TPayload, TConsumer> : BackgroundService
    where TConsumer : class, IEventConsumer<TPayload>
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly string _exchange;
    private readonly string _eventName;
    private readonly string _queueName;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqEventConsumerHost<TPayload, TConsumer>> _logger;

    private IChannel? _channel;

    public RabbitMqEventConsumerHost(
        RabbitMqConnectionProvider connectionProvider,
        string exchange,
        string eventName,
        string queueName,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqEventConsumerHost<TPayload, TConsumer>> logger)
    {
        _connectionProvider = connectionProvider;
        _exchange = exchange;
        _eventName = eventName;
        _queueName = queueName;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connectionProvider.CreateChannelAsync(stoppingToken);

        await _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        var dlx = $"{_exchange}.dlx";
        var dlq = $"{_queueName}.dlq";

        await _channel.ExchangeDeclareAsync(dlx, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(dlq, dlx, _eventName, cancellationToken: stoppingToken);

        var args = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", dlx },
            { "x-dead-letter-routing-key", _eventName }
        };

        await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(_queueName, _exchange, _eventName, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var envelope = JsonSerializer.Deserialize<EventEnvelopeV1<TPayload>>(body, RealEstateCrmJsonDefaults.Options);
                if (envelope == null) throw new InvalidOperationException("Failed to deserialize event envelope");

                using var scope = _serviceProvider.CreateScope();
                var consumerImpl = scope.ServiceProvider.GetRequiredService<TConsumer>();
                var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();

                var shouldProcess = await inbox.TryMarkConsumedAsync(envelope.EventId, consumerImpl.ConsumerName, CancellationToken.None);
                if (shouldProcess)
                {
                    await consumerImpl.HandleAsync(envelope, CancellationToken.None);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event from queue {QueueName}", _queueName);
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
