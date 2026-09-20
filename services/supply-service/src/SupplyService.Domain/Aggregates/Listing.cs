using System;

namespace SupplyService.Domain.Aggregates;

public record Listing(
    string ListingId,
    string PropertyId,
    string OperationTypeCode,
    string CanonicalTitle,
    string? CanonicalDescription,
    ListingCommercialTerms CommercialTerms,
    ListingAvailability Availability,
    string? ResponsibleUserId,
    string Status,
    int CatalogVersion,
    long Version)
{
    public static Listing Create(string listingId, string propertyId, string operationTypeCode, string canonicalTitle, string? canonicalDescription, ListingCommercialTerms commercialTerms, ListingAvailability availability, string? responsibleUserId)
    {
        return new Listing(listingId, propertyId, operationTypeCode, canonicalTitle, canonicalDescription, commercialTerms, availability, responsibleUserId, "DRAFT", 1, 1);
    }

    public Listing Activate()
    {
        if (Status == "CLOSED") throw new InvalidOperationException("Cannot activate a closed listing");
        return this with { Status = "ACTIVE", Version = this.Version + 1 };
    }

    public Listing Pause()
    {
        if (Status == "CLOSED") throw new InvalidOperationException("Cannot pause a closed listing");
        return this with { Status = "PAUSED", Version = this.Version + 1 };
    }

    public Listing Close()
    {
        return this with { Status = "CLOSED", Version = this.Version + 1 };
    }

    public Listing Update(string canonicalTitle, string? canonicalDescription, ListingCommercialTerms commercialTerms, ListingAvailability availability, string? responsibleUserId)
    {
        if (Status == "CLOSED") throw new InvalidOperationException("Cannot update a closed listing");
        return this with 
        { 
            CanonicalTitle = canonicalTitle, 
            CanonicalDescription = canonicalDescription, 
            CommercialTerms = commercialTerms, 
            Availability = availability, 
            ResponsibleUserId = responsibleUserId,
            Version = this.Version + 1 
        };
    }
}

public record Money(decimal Amount, string Currency);
public record ListingCommercialTerms(Money Price, Money? Expenses);
public record ListingAvailability(DateTimeOffset AvailableFrom, string Status);
