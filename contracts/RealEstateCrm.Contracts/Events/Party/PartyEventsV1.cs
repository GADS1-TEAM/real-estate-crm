namespace RealEstateCrm.Contracts.Events.Party;

/// <summary>
/// Payloads v1 de <c>EventEnvelopeV1</c> publicados por <c>party-service</c> (exchange
/// <c>party-service.events</c>, routing key = nombre del evento). Payload mínimo: sin CUIT,
/// documento, email ni teléfono. <c>PartyResponsibleAssigned</c> reutiliza
/// <see cref="Access.ResponsibleAssignedV1"/> con <c>resourceType = "party"</c>.
/// </summary>
public sealed record PartyRegisteredV1(
    Guid PartyId,
    string Kind,
    string DisplayName,
    string IdentityStatus,
    string CommercialStatus,
    Guid? ResponsibleUserId,
    string? OriginCode,
    int? OriginCatalogVersion);

/// <summary>Solo nombres de campos cambiados (<paramref name="ChangedFields"/>), nunca sus valores sensibles.</summary>
public sealed record PartyUpdatedV1(Guid PartyId, string Kind, string DisplayName, IReadOnlyList<string> ChangedFields);

public sealed record PartyRelationshipCreatedV1(
    Guid RelationshipId,
    Guid FromPartyId,
    Guid ToPartyId,
    string RelationshipType,
    DateTimeOffset ValidFrom);

public sealed record PartyCommercialStatusChangedV1(Guid PartyId, string PreviousStatus, string NewStatus);
