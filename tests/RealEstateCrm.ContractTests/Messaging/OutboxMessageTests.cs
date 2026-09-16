using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Events;

namespace RealEstateCrm.ContractTests.Messaging;

public class OutboxMessageTests
{
    private sealed record SamplePayload(string PartyName);

    [Fact]
    public void From_copies_envelope_metadata_and_serializes_the_full_envelope()
    {
        var envelope = new EventEnvelopeV1<SamplePayload>(
            EventId: Guid.NewGuid(),
            Name: "PartyRegistered",
            Version: 1,
            OccurredAt: DateTimeOffset.UtcNow,
            ActorId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid(),
            AggregateId: Guid.NewGuid(),
            Payload: new SamplePayload("Empresa Demo SA"));

        var createdAt = DateTimeOffset.UtcNow;
        var message = OutboxMessage.From(envelope, createdAt);

        Assert.Equal(envelope.EventId, message.EventId);
        Assert.Equal(envelope.Name, message.Name);
        Assert.Equal(envelope.Version, message.Version);
        Assert.Equal(envelope.ActorId, message.ActorId);
        Assert.Equal(envelope.CorrelationId, message.CorrelationId);
        Assert.Equal(envelope.CausationId, message.CausationId);
        Assert.Equal(envelope.AggregateId, message.AggregateId);
        Assert.Equal(createdAt, message.CreatedAt);
        Assert.False(message.IsPublished);
        Assert.Contains("Empresa Demo SA", message.EnvelopeJson);
        Assert.Contains(envelope.EventId.ToString(), message.EnvelopeJson);
    }
}
