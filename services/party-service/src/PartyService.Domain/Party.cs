namespace PartyService.Domain;

/// <summary>Empresa (<see cref="LegalEntity"/>) o Contacto (<see cref="NaturalPerson"/>): una única Party por identidad.</summary>
public enum PartyKind
{
    LegalEntity,
    NaturalPerson,
}

/// <summary>
/// Ciclo técnico de identidad del owner. V2 solo genera <see cref="Active"/>; <see cref="Aliased"/>
/// existe en el modelo pero nunca se produce (sin Identity Resolution automática).
/// </summary>
public enum IdentityStatus
{
    Provisional,
    Active,
    Aliased,
    Inactive,
    Restricted,
}

/// <summary>Estado comercial, independiente de <see cref="IdentityStatus"/>. La baja lógica es <see cref="Inactive"/>.</summary>
public enum CommercialStatus
{
    Potential,
    Customer,
    Inactive,
    DoNotContact,
}

/// <summary>Datos de perfil editables de una Party. Todo es opcional salvo <see cref="DisplayName"/> (captura mínima).</summary>
public sealed record PartyProfile(
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
    string? Notes);

/// <summary>
/// Aggregate root de Party (PTY-001..PTY-007). Empresa y Contacto comparten el modelo; no es
/// Organization/tenant. Sin unicidad de CUIT/email/teléfono ni resolución de duplicados.
/// </summary>
/// <remarks>
/// Inmutable (record): cada mutación devuelve una nueva instancia con <see cref="Version"/> + 1
/// que el repositorio versionado persiste con concurrencia optimista. La baja es lógica
/// (<see cref="CommercialStatus.Inactive"/>): la Party no se borra nunca. <c>CreatedBy</c>,
/// <c>UpdatedBy</c> y <c>CommercialStatusChangedBy</c> son el <c>sub</c> del actor;
/// <see cref="ResponsibleUserId"/> es el <c>userId</c> de access-service.
/// </remarks>
public sealed record Party(
    Guid PartyId,
    PartyKind Kind,
    PartyProfile Profile,
    IdentityStatus IdentityStatus,
    CommercialStatus CommercialStatus,
    Guid? ResponsibleUserId,
    string? OriginCode,
    int? OriginCatalogVersion,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset UpdatedAt,
    Guid UpdatedBy,
    DateTimeOffset CommercialStatusChangedAt,
    Guid CommercialStatusChangedBy,
    int Version)
{
    /// <summary>Alta normal de V2: <c>identityStatus = ACTIVE</c>, <c>commercialStatus = POTENTIAL</c>.</summary>
    public static Party Register(
        PartyKind kind,
        PartyProfile profile,
        Guid responsibleUserId,
        string? originCode,
        int? originCatalogVersion,
        Guid actorId,
        DateTimeOffset now)
    {
        RequireDisplayName(profile);

        return new Party(
            Guid.NewGuid(),
            kind,
            profile,
            IdentityStatus.Active,
            CommercialStatus.Potential,
            responsibleUserId,
            originCode,
            originCatalogVersion,
            CreatedAt: now,
            CreatedBy: actorId,
            UpdatedAt: now,
            UpdatedBy: actorId,
            CommercialStatusChangedAt: now,
            CommercialStatusChangedBy: actorId,
            Version: 1);
    }

    /// <summary>Modifica datos de perfil y origen sin tocar identidad, estado ni responsable (PTY-002/PTY-004).</summary>
    public Party UpdateData(PartyProfile profile, string? originCode, int? originCatalogVersion, Guid actorId, DateTimeOffset now)
    {
        RequireDisplayName(profile);

        return this with
        {
            Profile = profile,
            OriginCode = originCode,
            OriginCatalogVersion = originCatalogVersion,
            UpdatedAt = now,
            UpdatedBy = actorId,
            Version = Version + 1,
        };
    }

    /// <summary>Cambia el estado comercial (incluye baja lógica y reactivación). No borra ni oculta nada.</summary>
    /// <exception cref="InvalidOperationException">Si el estado pedido es el actual.</exception>
    public Party ChangeCommercialStatus(CommercialStatus newStatus, Guid actorId, DateTimeOffset now)
    {
        if (newStatus == CommercialStatus)
        {
            throw new InvalidOperationException($"La party ya está en estado comercial {newStatus}.");
        }

        return this with
        {
            CommercialStatus = newStatus,
            CommercialStatusChangedAt = now,
            CommercialStatusChangedBy = actorId,
            UpdatedAt = now,
            UpdatedBy = actorId,
            Version = Version + 1,
        };
    }

    /// <summary>Asigna o reasigna el responsable (PTY-007).</summary>
    /// <exception cref="InvalidOperationException">Si el responsable pedido ya lo es.</exception>
    public Party AssignResponsible(Guid newResponsibleUserId, Guid actorId, DateTimeOffset now)
    {
        if (ResponsibleUserId == newResponsibleUserId)
        {
            throw new InvalidOperationException("El usuario indicado ya es el responsable de la party.");
        }

        return this with
        {
            ResponsibleUserId = newResponsibleUserId,
            UpdatedAt = now,
            UpdatedBy = actorId,
            Version = Version + 1,
        };
    }

    /// <summary>Nombres de los campos de perfil/origen que difieren entre esta Party y <paramref name="updated"/>.</summary>
    public IReadOnlyList<string> ChangedFields(Party updated)
    {
        var changed = new List<string>();

        void Compare(string name, string? before, string? after)
        {
            if (!string.Equals(before, after, StringComparison.Ordinal))
            {
                changed.Add(name);
            }
        }

        Compare("displayName", Profile.DisplayName, updated.Profile.DisplayName);
        Compare("legalName", Profile.LegalName, updated.Profile.LegalName);
        Compare("givenNames", Profile.GivenNames, updated.Profile.GivenNames);
        Compare("familyNames", Profile.FamilyNames, updated.Profile.FamilyNames);
        Compare("taxIdentifier", Profile.TaxIdentifier, updated.Profile.TaxIdentifier);
        Compare("identityDocument", Profile.IdentityDocument, updated.Profile.IdentityDocument);
        Compare("email", Profile.Email, updated.Profile.Email);
        Compare("phone", Profile.Phone, updated.Profile.Phone);
        Compare("address", Profile.Address, updated.Profile.Address);
        Compare("industry", Profile.Industry, updated.Profile.Industry);
        Compare("notes", Profile.Notes, updated.Profile.Notes);
        Compare("originCode", OriginCode, updated.OriginCode);

        return changed;
    }

    private static void RequireDisplayName(PartyProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            throw new ArgumentException("El nombre de la party es obligatorio.", nameof(profile));
        }
    }
}
