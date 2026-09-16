namespace RealEstateCrm.Contracts.Events;

/// <summary>
/// Envelope v1 para eventos de integración públicos. Todo evento publicado por un servicio V2
/// debe viajar envuelto en este contrato.
/// </summary>
/// <remarks>
/// No incluye <c>tenantId</c> (ADR-001, sección 1). Un cambio incompatible en este contrato
/// (renombrar/quitar un campo obligatorio) debe romper el contract test de
/// <c>RealEstateCrm.ContractTests</c>.
/// </remarks>
/// <typeparam name="TPayload">Forma del payload propio del evento.</typeparam>
/// <param name="EventId">Identificador único del evento. Clave de idempotencia para el Inbox.</param>
/// <param name="Name">Nombre del evento en PascalCase, hecho ocurrido (ej. "PartyRegistered").</param>
/// <param name="Version">Versión del contrato del evento, empieza en 1.</param>
/// <param name="OccurredAt">Momento en que ocurrió el hecho de negocio.</param>
/// <param name="ActorId">Actor que originó el hecho.</param>
/// <param name="CorrelationId">Identificador de la operación de negocio de punta a punta.</param>
/// <param name="CausationId">Identificador del comando/evento que causó este evento, si aplica.</param>
/// <param name="AggregateId">Identificador del aggregate root dueño del hecho.</param>
/// <param name="Payload">Datos propios del evento. Mínimos: no replica el aggregate completo ni PII innecesaria.</param>
public sealed record EventEnvelopeV1<TPayload>(
    Guid EventId,
    string Name,
    int Version,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AggregateId,
    TPayload Payload);
