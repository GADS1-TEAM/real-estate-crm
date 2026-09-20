using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Consumer;
using RealEstateCrm.TestSupport.Messaging;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Messaging.Consumer;

[Trait("Category", "RequiresRabbitMq")]
[Trait("Category", "RequiresMongo")]
public class EventConsumerTests
{
    private sealed record TestPayload(string Value);

    private sealed class TestEventConsumer : IEventConsumer<TestPayload>
    {
        public string ConsumerName => "TestConsumer";
        
        public static readonly List<EventEnvelopeV1<TestPayload>> Received = new();

        public Task HandleAsync(EventEnvelopeV1<TestPayload> envelope, CancellationToken cancellationToken = default)
        {
            Received.Add(envelope);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Consumer_Receives_Published_Event_And_Is_Idempotent()
    {
        // Setup
        var services = new ServiceCollection();
        
        services.AddLogging();
        
        var options = new RabbitMqOptions 
        { 
            ServiceExchangeName = "test.exchange",
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var p) ? p : 5672,
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
            VirtualHost = "/"
        };
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<RabbitMqConnectionProvider>();

        services.AddSingleton<IInbox, FakeInbox>();

        services.AddEventConsumer<TestPayload, TestEventConsumer>("test.exchange", "TestEvent", "test.queue");

        var sp = services.BuildServiceProvider();
        
        var hostedServices = sp.GetServices<IHostedService>();
        foreach(var hs in hostedServices)
        {
            await hs.StartAsync(CancellationToken.None);
        }

        var provider = sp.GetRequiredService<RabbitMqConnectionProvider>();
        var publisher = new RabbitMqEventPublisher(provider, Options.Create(options));
        
        var eventId = Guid.NewGuid();
        var envelope = new EventEnvelopeV1<TestPayload>(
            eventId,
            "TestEvent",
            1,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            new TestPayload("hello")
        );
        
        var outboxMessage = OutboxMessage.From(envelope, DateTimeOffset.UtcNow);
        
        await publisher.PublishAsync(outboxMessage, CancellationToken.None);
        
        // Act - polling wait
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while(TestEventConsumer.Received.Count == 0 && !cts.IsCancellationRequested)
        {
            await Task.Delay(100);
        }

        // Assert received
        Assert.Single(TestEventConsumer.Received);
        Assert.Equal("hello", TestEventConsumer.Received[0].Payload.Value);

        // Assert idempotency
        await publisher.PublishAsync(outboxMessage, CancellationToken.None);
        
        await Task.Delay(1000); // give it time to process duplicate
        
        Assert.Single(TestEventConsumer.Received); // still 1

        foreach(var hs in hostedServices)
        {
            await hs.StopAsync(CancellationToken.None);
        }
    }
}
