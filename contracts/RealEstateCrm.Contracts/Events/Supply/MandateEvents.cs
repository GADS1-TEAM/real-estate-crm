using System;
using System.Collections.Generic;
using RealEstateCrm.Contracts.Context;

namespace RealEstateCrm.Contracts.Events.Supply;

public record CommercialMandateActivatedV1(
    string MandateId,
    string PropertyId,
    List<Guid> GrantingPartyIds,
    List<string> AuthorizedOperationTypeCodes,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    string Status);
