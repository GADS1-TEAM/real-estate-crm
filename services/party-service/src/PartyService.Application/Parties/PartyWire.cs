using PartyService.Domain;
using RealEstateCrm.Contracts.Parties;

namespace PartyService.Application.Parties;

/// <summary>Traducción entre los enums del dominio y los textos SCREAMING_SNAKE del contrato público.</summary>
public static class PartyWire
{
    public static string ToWire(PartyKind kind) => kind switch
    {
        PartyKind.LegalEntity => PartyKinds.LegalEntity,
        PartyKind.NaturalPerson => PartyKinds.NaturalPerson,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string ToWire(IdentityStatus status) => status switch
    {
        IdentityStatus.Provisional => IdentityStatuses.Provisional,
        IdentityStatus.Active => IdentityStatuses.Active,
        IdentityStatus.Aliased => IdentityStatuses.Aliased,
        IdentityStatus.Inactive => IdentityStatuses.Inactive,
        IdentityStatus.Restricted => IdentityStatuses.Restricted,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static string ToWire(CommercialStatus status) => status switch
    {
        CommercialStatus.Potential => CommercialStatuses.Potential,
        CommercialStatus.Customer => CommercialStatuses.Customer,
        CommercialStatus.Inactive => CommercialStatuses.Inactive,
        CommercialStatus.DoNotContact => CommercialStatuses.DoNotContact,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static string ToWire(RelationshipType type) => type switch
    {
        RelationshipType.ContactOf => RelationshipTypes.ContactOf,
        RelationshipType.Represents => RelationshipTypes.Represents,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static bool TryParseCommercialStatus(string? value, out CommercialStatus status)
    {
        status = value switch
        {
            CommercialStatuses.Potential => CommercialStatus.Potential,
            CommercialStatuses.Customer => CommercialStatus.Customer,
            CommercialStatuses.Inactive => CommercialStatus.Inactive,
            CommercialStatuses.DoNotContact => CommercialStatus.DoNotContact,
            _ => default,
        };

        return CommercialStatuses.IsValid(value);
    }

    public static bool TryParseRelationshipType(string? value, out RelationshipType type)
    {
        type = value switch
        {
            RelationshipTypes.ContactOf => RelationshipType.ContactOf,
            RelationshipTypes.Represents => RelationshipType.Represents,
            _ => default,
        };

        return RelationshipTypes.IsValid(value);
    }

    public static bool TryParseKind(string? value, out PartyKind kind)
    {
        kind = value switch
        {
            PartyKinds.LegalEntity => PartyKind.LegalEntity,
            PartyKinds.NaturalPerson => PartyKind.NaturalPerson,
            _ => default,
        };

        return value is PartyKinds.LegalEntity or PartyKinds.NaturalPerson;
    }
}
