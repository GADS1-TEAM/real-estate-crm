using System.Text.Json;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Events;

public class EventEnvelopeV1ContractTests
{
    private sealed record SamplePayload(string PartyName);

    /// <summary>
    /// JSON "congelado" de un EventEnvelopeV1&lt;SamplePayload&gt; válido para este contrato v1.
    /// Si CONT-002 cambia de forma incompatible (renombra/quita un campo obligatorio), la
    /// deserialización deja el campo en su default y las asserts de abajo fallan.
    /// </summary>
    private const string FrozenEnvelopeJson = """
        {
          "eventId": "8f14e45f-ceea-4a1f-8f5b-6c2c3b6a7b00",
          "name": "PartyRegistered",
          "version": 1,
          "occurredAt": "2026-09-16T12:00:00+00:00",
          "actorId": "3c2f2e10-9b1a-4a3e-8b2a-2a6f1e6a5b11",
          "correlationId": "1a2b3c4d-5e6f-4a1b-9c2d-3e4f5a6b7c22",
          "causationId": "5e6f7a8b-9c0d-4e1f-a2b3-c4d5e6f7a833",
          "aggregateId": "9c0d1e2f-3a4b-4c5d-8e9f-0a1b2c3d4e44",
          "payload": { "partyName": "Empresa Demo SA" }
        }
        """;

    [Fact]
    public void Deserializes_frozen_json_with_actor_and_correlation_metadata()
    {
        var envelope = JsonSerializer.Deserialize<EventEnvelopeV1<SamplePayload>>(
            FrozenEnvelopeJson, RealEstateCrmJsonDefaults.Options);

        Assert.NotNull(envelope);
        Assert.Equal(Guid.Parse("8f14e45f-ceea-4a1f-8f5b-6c2c3b6a7b00"), envelope.EventId);
        Assert.Equal("PartyRegistered", envelope.Name);
        Assert.Equal(1, envelope.Version);
        Assert.Equal(DateTimeOffset.Parse("2026-09-16T12:00:00+00:00"), envelope.OccurredAt);
        Assert.Equal(Guid.Parse("3c2f2e10-9b1a-4a3e-8b2a-2a6f1e6a5b11"), envelope.ActorId);
        Assert.Equal(Guid.Parse("1a2b3c4d-5e6f-4a1b-9c2d-3e4f5a6b7c22"), envelope.CorrelationId);
        Assert.Equal(Guid.Parse("5e6f7a8b-9c0d-4e1f-a2b3-c4d5e6f7a833"), envelope.CausationId);
        Assert.Equal(Guid.Parse("9c0d1e2f-3a4b-4c5d-8e9f-0a1b2c3d4e44"), envelope.AggregateId);
        Assert.Equal("Empresa Demo SA", envelope.Payload.PartyName);
    }

    [Fact]
    public void Round_trips_through_serialize_and_deserialize()
    {
        var original = new EventEnvelopeV1<SamplePayload>(
            EventId: Guid.NewGuid(),
            Name: "ListingPublished",
            Version: 1,
            OccurredAt: DateTimeOffset.UtcNow,
            ActorId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid(),
            AggregateId: Guid.NewGuid(),
            Payload: new SamplePayload("Casa en venta"));

        var json = JsonSerializer.Serialize(original, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<EventEnvelopeV1<SamplePayload>>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void Serialized_shape_exposes_all_required_fields_as_camelCase()
    {
        var envelope = new EventEnvelopeV1<SamplePayload>(
            Guid.NewGuid(), "PartyRegistered", 1, DateTimeOffset.UtcNow,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new SamplePayload("x"));

        var json = JsonSerializer.Serialize(envelope, RealEstateCrmJsonDefaults.Options);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        foreach (var expectedField in new[]
        {
            "eventId", "name", "version", "occurredAt", "actorId",
            "correlationId", "causationId", "aggregateId", "payload",
        })
        {
            Assert.True(root.TryGetProperty(expectedField, out _), $"Falta el campo '{expectedField}' en el EventEnvelopeV1 serializado.");
        }

        Assert.False(root.TryGetProperty("tenantId", out _), "EventEnvelopeV1 no debe exponer tenantId (ADR-001).");
        Assert.False(root.TryGetProperty("organizationId", out _), "EventEnvelopeV1 no debe exponer organizationId (ADR-001).");
    }
}
