using System;
using System.Collections.Generic;

namespace SupplyService.Domain.Aggregates;

public record CommercialMandate(
    string MandateId,
    string PropertyId,
    List<Guid> GrantingPartyIds,
    List<string> AuthorizedOperationTypeCodes,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    string Status,
    long Version)
{
    public static CommercialMandate Grant(string mandateId, string propertyId, List<Guid> grantingPartyIds, List<string> authorizedOperationTypeCodes, DateTimeOffset validFrom, DateTimeOffset? validUntil)
    {
        return new CommercialMandate(mandateId, propertyId, grantingPartyIds, authorizedOperationTypeCodes, validFrom, validUntil, "ACTIVE", 1);
    }
}
