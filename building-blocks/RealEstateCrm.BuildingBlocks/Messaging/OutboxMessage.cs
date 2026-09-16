using System.Text.Json;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.BuildingBlocks.Messaging;

/// <summary>
/// Representación persistida de un <see cref="EventEnvelopeV1{TPayload}"/> pendiente de publicar.
/// </summary>
/// <remarks>
/// Guarda la metadata como campos propios (para poder consultarla/indexarla en Mongo sin
/// deserializar) y también el envelope completo ya serializado en <see cref="EnvelopeJson"/>,
/// listo para publicarse tal cual como body del mensaje de RabbitMQ.
/// </remarks>
public sealed record OutboxMessage(
    Guid EventId,
    string Name,
    int Version,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AggregateId,
    string EnvelopeJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt = null)
{
    /// <summary>Construye el <see cref="OutboxMessage"/> a partir del envelope tipado, serializándolo con las opciones JSON estándar de contracts.</summary>
    public static OutboxMessage From<TPayload>(EventEnvelopeV1<TPayload> envelope, DateTimeOffset createdAt) =>
        new(
            envelope.EventId,
            envelope.Name,
            envelope.Version,
            envelope.OccurredAt,
            envelope.ActorId,
            envelope.CorrelationId,
            envelope.CausationId,
            envelope.AggregateId,
            JsonSerializer.Serialize(envelope, RealEstateCrmJsonDefaults.Options),
            createdAt);

    public bool IsPublished => PublishedAt is not null;
}
