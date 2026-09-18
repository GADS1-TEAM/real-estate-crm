namespace RealEstateCrm.Contracts.Parties;

/// <summary>Tipos de Party (V2-PTY-001): Empresa = <c>LEGAL_ENTITY</c>, Contacto = <c>NATURAL_PERSON</c>.</summary>
public static class PartyKinds
{
    public const string LegalEntity = "LEGAL_ENTITY";
    public const string NaturalPerson = "NATURAL_PERSON";
}

/// <summary>
/// Ciclo técnico de identidad del owner. Dimensión independiente de <see cref="CommercialStatuses"/>.
/// V2 solo genera <see cref="Active"/>; <see cref="Aliased"/> nunca se genera (sin Identity Resolution).
/// </summary>
public static class IdentityStatuses
{
    public const string Provisional = "PROVISIONAL";
    public const string Active = "ACTIVE";
    public const string Aliased = "ALIASED";
    public const string Inactive = "INACTIVE";
    public const string Restricted = "RESTRICTED";
}

/// <summary>Los cuatro estados comerciales requeridos por V2. El alta es <see cref="Potential"/>.</summary>
public static class CommercialStatuses
{
    public const string Potential = "POTENTIAL";
    public const string Customer = "CUSTOMER";
    public const string Inactive = "INACTIVE";
    public const string DoNotContact = "DO_NOT_CONTACT";

    public static IReadOnlyList<string> All { get; } = new[] { Potential, Customer, Inactive, DoNotContact };

    public static bool IsValid(string? value) => value is not null && All.Contains(value, StringComparer.Ordinal);
}

/// <summary>Tipos de relación entre Parties. <c>fromPartyId</c> es el Contacto y <c>toPartyId</c> la Empresa.</summary>
public static class RelationshipTypes
{
    public const string ContactOf = "CONTACT_OF";
    public const string Represents = "REPRESENTS";

    public static IReadOnlyList<string> All { get; } = new[] { ContactOf, Represents };

    public static bool IsValid(string? value) => value is not null && All.Contains(value, StringComparer.Ordinal);
}
