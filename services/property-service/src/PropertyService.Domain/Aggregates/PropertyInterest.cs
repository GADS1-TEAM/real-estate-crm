using System;

namespace PropertyService.Domain.Aggregates;

public record PropertyInterest(
    string InterestId,
    string PropertyId,
    string HolderPartyId,
    string RightType,
    decimal? Participation,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    string Status)
{
    public static PropertyInterest Create(string interestId, string propertyId, string holderPartyId, string rightType, decimal? participation)
    {
        return new PropertyInterest(interestId, propertyId, holderPartyId, rightType, participation, DateTimeOffset.UtcNow, null, "ACTIVE");
    }
}
