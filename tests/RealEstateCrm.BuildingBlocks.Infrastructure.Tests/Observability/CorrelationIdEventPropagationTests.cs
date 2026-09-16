using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;
using RealEstateCrm.BuildingBlocks.Infrastructure.Observability;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Events;
using MongoDB.Driver;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Observability;

/// <summary>
/// OPS-004 a nivel building block: el correlationId de un <see cref="EventEnvelopeV1{TPayload}"/>
/// sobrevive publish (outbox → RabbitMQ) → consume, y aparece logueado en ambos extremos,
/// mientras un campo sensible del payload no llega en texto plano al log.
/// </summary>
/// <remarks>
/// No es la demostración end-to-end real "BFF → consumer" que pide V2-FND-003: ese slice no
/// existe todavía (ningún <c>Program.cs</c> de <c>services/</c>/<c>bffs/</c> está wireado, ver
/// IMPLEMENTATION_REPORT-V2-FND-003.md). Queda como follow-up para el primer slice real
/// (V2-ACL-001), que es quien recién va a tener un endpoint HTTP real emitiendo el evento.
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresRabbitMq"</c>.
/// </remarks>
[Trait("Category", "RequiresRabbitMq")]
public class CorrelationIdEventPropagationTests : IAsyncLifetime
{
    private static readonly string RabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    private static readonly string MongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017/?directConnection=true";

    private RabbitMqConnectionProvider _connectionProvider = null!;
    private IMongoClient _mongoClient = null!;
    private string _mongoDatabaseName = null!;

    public Task InitializeAsync()
    {
        _connectionProvider = new RabbitMqConnectionProvider(Options.Create(new RabbitMqOptions { HostName = RabbitMqHost }));
        _mongoClient = new MongoClient(MongoConnectionString);
        _mongoDatabaseName = "fnd003_correlation_test_" + Guid.NewGuid().ToString("N");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _connectionProvider.DisposeAsync();
        await _mongoClient.DropDatabaseAsync(_mongoDatabaseName);
    }

    private sealed record RequirementPayload(string PartyDisplayName, string Documento);

    private sealed class RecordingConsumer(TextWriter logOutput) : IEventConsumer<RequirementPayload>
    {
        public string ConsumerName => "fnd003-correlation-test-consumer";
        private readonly TaskCompletionSource _received = new();

        public Task HandleAsync(EventEnvelopeV1<RequirementPayload> envelope, CancellationToken cancellationToken = default)
        {
            var message = SensitiveDataRedactor.Redact(
                $"Consumed {envelope.Name} correlationId={envelope.CorrelationId} documento={envelope.Payload.Documento}");
            logOutput.WriteLine(message);
            _received.TrySetResult();
            return Task.CompletedTask;
        }

        public Task WaitForMessageAsync(TimeSpan timeout) => _received.Task.WaitAsync(timeout);
    }

    [Fact]
    public async Task CorrelationId_survives_publish_to_consume_and_sensitive_payload_fields_are_never_logged_raw()
    {
        var exchangeName = "fnd003-correlation-exchange-" + Guid.NewGuid().ToString("N")[..8];
        var queueName = "fnd003-correlation-queue-" + Guid.NewGuid().ToString("N")[..8];
        var correlationId = Guid.NewGuid();
        var logOutput = new StringWriter();

        var publisher = new RabbitMqEventPublisher(_connectionProvider, Options.Create(new RabbitMqOptions { ServiceExchangeName = exchangeName }));
        var inbox = new MongoInbox(_mongoClient.GetDatabase(_mongoDatabaseName));
        var host = new RabbitMqEventConsumerHost(_connectionProvider);
        var consumer = new RecordingConsumer(logOutput);

        var registration = new RabbitMqConsumerRegistration(exchangeName, "RequirementRegistered", queueName);
        await using var channel = await host.StartAsync(registration, consumer, inbox);

        var envelope = new EventEnvelopeV1<RequirementPayload>(
            Guid.NewGuid(), "RequirementRegistered", 1, DateTimeOffset.UtcNow,
            Guid.NewGuid(), correlationId, null, Guid.NewGuid(),
            new RequirementPayload("Ana Vendedora", Documento: "30111222"));

        var publishLogMessage = SensitiveDataRedactor.Redact(
            $"Publishing {envelope.Name} correlationId={envelope.CorrelationId} documento={envelope.Payload.Documento}");
        logOutput.WriteLine(publishLogMessage);

        await publisher.PublishAsync(OutboxMessage.From(envelope, DateTimeOffset.UtcNow));

        await consumer.WaitForMessageAsync(TimeSpan.FromSeconds(10));

        var written = logOutput.ToString();
        Assert.Contains($"correlationId={correlationId}", written);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(written, System.Text.RegularExpressions.Regex.Escape(correlationId.ToString())).Count);
        Assert.DoesNotContain("30111222", written);
    }
}
