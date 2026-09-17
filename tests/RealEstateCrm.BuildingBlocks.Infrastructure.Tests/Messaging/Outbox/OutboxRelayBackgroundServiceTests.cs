using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Outbox;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Events;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Messaging.Outbox;

/// <summary>
/// Relay reutilizable del outbox (V2-ACL-001, pedido explícito): drena mensajes pendientes de un
/// <see cref="MongoOutbox"/> real y los publica con un <see cref="RabbitMqEventPublisher"/> real,
/// verificando que el consumidor los recibe y que el outbox queda marcado como publicado.
/// </summary>
/// <remarks>
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresMongo&amp;Category!=RequiresRabbitMq"</c>.
/// Necesita Mongo (<c>docker run --rm -p 27017:27017 mongo:7</c>) y RabbitMQ
/// (<c>docker run --rm -p 5672:5672 rabbitmq:4-management</c>) corriendo.
/// </remarks>
[Trait("Category", "RequiresMongo")]
[Trait("Category", "RequiresRabbitMq")]
public class OutboxRelayBackgroundServiceTests : IAsyncLifetime
{
    private static readonly string MongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017";
    private static readonly string RabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";

    private IMongoClient _mongoClient = null!;
    private string _mongoDatabaseName = null!;
    private RabbitMqConnectionProvider _connectionProvider = null!;

    public Task InitializeAsync()
    {
        _mongoClient = new MongoClient(MongoConnectionString);
        _mongoDatabaseName = "acl001_outbox_relay_test_" + Guid.NewGuid().ToString("N");
        _connectionProvider = new RabbitMqConnectionProvider(Options.Create(new RabbitMqOptions { HostName = RabbitMqHost }));
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _mongoClient.DropDatabaseAsync(_mongoDatabaseName);
        await _connectionProvider.DisposeAsync();
    }

    private sealed record SamplePayload(string DisplayName);

    private sealed class RecordingConsumer : IEventConsumer<SamplePayload>
    {
        public string ConsumerName => "outbox-relay-test-consumer";
        public readonly List<EventEnvelopeV1<SamplePayload>> Received = [];
        private readonly TaskCompletionSource _firstMessageReceived = new();

        public Task HandleAsync(EventEnvelopeV1<SamplePayload> envelope, CancellationToken cancellationToken = default)
        {
            Received.Add(envelope);
            _firstMessageReceived.TrySetResult();
            return Task.CompletedTask;
        }

        public Task WaitForFirstMessageAsync(TimeSpan timeout) => _firstMessageReceived.Task.WaitAsync(timeout);
    }

    [Fact]
    public async Task Relay_publishes_a_pending_message_and_marks_it_published()
    {
        var database = _mongoClient.GetDatabase(_mongoDatabaseName);
        var sessionAccessor = new MongoSessionAccessor();
        var outbox = new MongoOutbox(database, sessionAccessor);
        var inbox = new MongoInbox(database);

        var exchangeName = "acl001-relay-test-exchange-" + Guid.NewGuid().ToString("N")[..8];
        var queueName = "acl001-relay-test-queue-" + Guid.NewGuid().ToString("N")[..8];

        var publisher = new RabbitMqEventPublisher(_connectionProvider, Options.Create(new RabbitMqOptions { ServiceExchangeName = exchangeName }));
        var host = new RabbitMqEventConsumerHost(_connectionProvider);
        var consumer = new RecordingConsumer();
        var registration = new RabbitMqConsumerRegistration(exchangeName, "UserCreated", queueName);
        await using var channel = await host.StartAsync(registration, consumer, inbox);

        var services = new ServiceCollection();
        services.AddScoped<IOutbox>(_ => outbox);
        services.AddSingleton<IEventPublisher>(publisher);
        await using var provider = services.BuildServiceProvider();

        var relay = new OutboxRelayBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxRelayOptions { PollingInterval = TimeSpan.FromMilliseconds(200), BatchSize = 10 }),
            NullLogger<OutboxRelayBackgroundService>.Instance);

        var envelope = new EventEnvelopeV1<SamplePayload>(
            Guid.NewGuid(), "UserCreated", 1, DateTimeOffset.UtcNow,
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), new SamplePayload("Nuevo Usuario"));
        await outbox.EnqueueAsync(OutboxMessage.From(envelope, DateTimeOffset.UtcNow));

        await relay.StartAsync(CancellationToken.None);
        try
        {
            await consumer.WaitForFirstMessageAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await relay.StopAsync(CancellationToken.None);
        }

        Assert.Single(consumer.Received);
        Assert.Equal(envelope.EventId, consumer.Received[0].EventId);

        var stillPending = await outbox.GetPendingAsync(batchSize: 10);
        Assert.DoesNotContain(stillPending, m => m.EventId == envelope.EventId);
    }
}
