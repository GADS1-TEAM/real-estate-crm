namespace RealEstateCrm.Contracts.Parties;

/// <summary>Fila de <c>GET /api/v1/parties</c> (SearchParties).</summary>
public sealed record PartySummaryV1(
    Guid PartyId,
    string Kind,
    string DisplayName,
    string? Email,
    string? Phone,
    string IdentityStatus,
    string CommercialStatus,
    Guid? ResponsibleUserId,
    string? OriginCode);

/// <summary>
/// Relación vista desde una Party concreta. <paramref name="RelatedPartyId"/> es el otro extremo
/// (para una Empresa, el Contacto; para un Contacto, la Empresa).
/// </summary>
public sealed record PartyRelationshipV1(
    Guid RelationshipId,
    Guid FromPartyId,
    Guid ToPartyId,
    string RelationshipType,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    Guid CreatedBy,
    Guid RelatedPartyId,
    string? RelatedPartyDisplayName);

/// <summary>
/// Detalle de Empresa o Contacto (GetCompanyDetail/GetContactDetail). <c>identityStatus</c> y
/// <c>commercialStatus</c> son campos separados. <c>createdBy</c>/<c>updatedBy</c>/
/// <c>commercialStatusChangedBy</c> son el <c>sub</c> del actor (misma convención que
/// <c>ExecutionContextV1.ActorId</c>); <c>responsibleUserId</c> es el <c>userId</c> de access-service.
/// </summary>
public sealed record PartyDetailV1(
    Guid PartyId,
    string Kind,
    string DisplayName,
    string? LegalName,
    string? GivenNames,
    string? FamilyNames,
    string? TaxIdentifier,
    string? IdentityDocument,
    string? Email,
    string? Phone,
    string? Address,
    string? Industry,
    string IdentityStatus,
    string CommercialStatus,
    Guid? ResponsibleUserId,
    string? OriginCode,
    int? OriginCatalogVersion,
    string? Notes,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset UpdatedAt,
    Guid UpdatedBy,
    DateTimeOffset CommercialStatusChangedAt,
    Guid CommercialStatusChangedBy,
    int Version,
    IReadOnlyList<PartyRelationshipV1> Relationships);
