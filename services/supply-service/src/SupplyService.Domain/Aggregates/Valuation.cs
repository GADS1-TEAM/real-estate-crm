using System;

namespace SupplyService.Domain.Aggregates;

public record Valuation(
    string ValuationId,
    string PropertyId,
    string PerformedBy,
    DateTimeOffset ValuationDate,
    decimal ValueAmount,
    string Currency,
    decimal? OwnerExpectedValueAmount,
    string? Confidence,
    long Version)
{
    public static Valuation Issue(string valuationId, string propertyId, string performedBy, DateTimeOffset valuationDate, decimal valueAmount, string currency, decimal? ownerExpectedValueAmount, string? confidence)
    {
        return new Valuation(valuationId, propertyId, performedBy, valuationDate, valueAmount, currency, ownerExpectedValueAmount, confidence, 1);
    }
}