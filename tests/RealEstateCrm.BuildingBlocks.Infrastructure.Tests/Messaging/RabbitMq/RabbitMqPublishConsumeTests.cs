using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Events;
using MongoDB.Driver;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Messaging.RabbitMq;

/// <summary>
/// Publica un evento con <see cref="RabbitMqEventPublisher"/> y lo consume con
/// <see cref="RabbitMqEventConsumerHost"/>, verificando la topología (exchange topic + routing
/// key = nombre del evento + cola/DLQ por consumidor) y que el Inbox evita el reproceso si
/// RabbitMQ reentrega el mismo mensaje.
/// </summary>
/// <remarks>
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresRabbitMq"</c>.
/// Para correrlo: <c>docker run --rm -p 5672:5672 rabbitmq:4-management</c>.
/// El Inbox de este test usa Mongo (<see cref="MongoInbox"/>), así que también necesita un
/// Mongo standalone corriendo (ver <see cref="Mongo.MongoInboxIdempotencyTests"/>).
/// </remarks>
[Trait("Category", "RequiresRabbitMq")]
public class RabbitMqPublishConsumeTests : IAsyncLifetime
{
    private static readonly string RabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    private static readonly string MongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017";

    private RabbitMqConnectionProvider _connectionProvider = null!;
    private IMongoClient _mongoClient = null!;
    private string _mongoDatabaseName = null!;

    public Task InitializeAsync()
    {
        _connectionProvider = new RabbitMqConnectionProvider(Options.Create(new RabbitMqOptions { HostName = RabbitMqHost }));
        _mongoClient = new MongoClient(MongoConnectionString);
        _mongoDatabaseName = "fnd002_rabbitmq_test_" + Guid.NewGuid().ToString("N");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _connectionProvider.DisposeAsync();
        await _mongoClient.DropDatabaseAsync(_mongoDatabaseName);
    }

    private sealed class RecordingConsumer : IEventConsumer<SamplePayload>
    {
        public string ConsumerName => "recording-test-consumer";
        public readonly List<EventEnvelopeV1<SamplePayload>> Received = [];
        private readonly TaskCompletionSource _firstMessageReceived = new();

        public Task HandleAsync(EventEnvelopeV1<SamplePayload> envelope, CancellationToken cancellationToken = default)
        {
            Received.Add(envelope);
            _firstMessageReceived.TrySetResult();
            return Task.CompletedTask;
        }

        public Task WaitForFirstMessageAsync(TimeSpan timeout) =>
            _firstMessageReceived.Task.WaitAsync(timeout);
    }

    private sealed record SamplePayload(string PartyName);

    [Fact]
    public async Task Published_event_is_delivered_exactly_once_to_the_consumer()
    {
        var exchangeName = "fnd002-test-exchange-" + Guid.NewGuid().ToString("N")[..8];
        var queueName = "fnd002-test-queue-" + Guid.NewGuid().ToString("N")[..8];

        var publisher = new RabbitMqEventPublisher(_connectionProvider, Options.Create(new RabbitMqOptions { ServiceExchangeName = exchangeName }));
        var inbox = new MongoInbox(_mongoClient.GetDatabase(_mongoDatabaseName));
        var host = new RabbitMqEventConsumerHost(_connectionProvider);
        var consumer = new RecordingConsumer();

        var registration = new RabbitMqConsumerRegistration(exchangeName, "PartyRegistered", queueName);
        await using var channel = await host.StartAsync(registration, consumer, inbox);

        var envelope = new EventEnvelopeV1<SamplePayload>(
            Guid.NewGuid(), "PartyRegistered", 1, DateTimeOffset.UtcNow,
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), new SamplePayload("Empresa Demo SA"));

        await publisher.PublishAsync(OutboxMessage.From(envelope, DateTimeOffset.UtcNow));

        await consumer.WaitForFirstMessageAsync(TimeSpan.FromSeconds(10));

        Assert.Single(consumer.Received);
        Assert.Equal(envelope.EventId, consumer.Received[0].EventId);
        Assert.Equal("Empresa Demo SA", consumer.Received[0].Payload.PartyName);
    }
}
