using System;
using RealEstateCrm.Contracts.Financial;
using RealEstateCrm.Contracts.Context;

namespace RealEstateCrm.Contracts.Events.Supply;

public record ValuationIssuedV1(
    string ValuationId,
    string PropertyId,
    string PerformedBy,
    DateTimeOffset ValuationDate,
    MoneyV1 Value,
    string Currency,
    MoneyV1? OwnerExpectedValue,
    string? Confidence,
    long Version);
