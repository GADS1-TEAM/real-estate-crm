namespace PartyService.Domain;

/// <summary>Tipo de relación. Se almacena siempre <c>from = Contacto</c>, <c>to = Empresa</c>.</summary>
public enum RelationshipType
{
    ContactOf,
    Represents,
}

/// <summary>
/// Relación Contacto→Empresa (PTY-005). Un Contacto puede existir sin ninguna y puede tener
/// relaciones con varias Empresas. Solo se inserta: V2 no expone cierre (<see cref="ValidTo"/>
/// queda disponible para una evolución) ni borrado.
/// </summary>
public sealed record PartyRelationship(
    Guid RelationshipId,
    Guid FromPartyId,
    Guid ToPartyId,
    RelationshipType RelationshipType,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    Guid CreatedBy)
{
    public static PartyRelationship Create(Guid contactId, Guid companyId, RelationshipType type, Guid actorId, DateTimeOffset now) =>
        new(Guid.NewGuid(), contactId, companyId, type, ValidFrom: now, ValidTo: null, CreatedBy: actorId);

    /// <summary>Vigente si no tiene <see cref="ValidTo"/> o todavía no venció.</summary>
    public bool IsCurrent(DateTimeOffset now) => ValidTo is null || ValidTo > now;
}
