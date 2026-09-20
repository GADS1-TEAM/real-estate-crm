using System;
using System.Collections.Generic;
using System.Linq;

namespace DemandService.Domain.Aggregates;

public record Requirement(
    string RequirementId,
    List<Guid> SeekerPartyIds,
    string OperationTypeCode,
    string? PropertyTypeCode,
    LocationCriteria? LocationCriteria,
    FinancialCriteria? FinancialCriteria,
    List<string> SelectedListingIds,
    string? ResponsibleUserId,
    string? OriginCode,
    CommercialProgress CommercialProgress,
    string Status,
    long Version)
{
    public static Requirement Create(string requirementId, List<Guid> seekerPartyIds, string operationTypeCode, string? propertyTypeCode, LocationCriteria? locationCriteria, FinancialCriteria? financialCriteria, string? responsibleUserId, string? originCode)
    {
        return new Requirement(
            requirementId,
            seekerPartyIds,
            operationTypeCode,
            propertyTypeCode,
            locationCriteria,
            financialCriteria,
            new List<string>(),
            responsibleUserId,
            originCode,
            new CommercialProgress("DEMAND_START", "1.0", DateTimeOffset.UtcNow, null),
            "DRAFT",
            1);
    }

    public Requirement Update(string status)
    {
        return this with { Status = status, Version = Version + 1 };
    }
    
    public Requirement Activate()
    {
        return this with { Status = "ACTIVE", Version = Version + 1 };
    }

    public Requirement AddSelectedListing(string listingId)
    {
        var listings = SelectedListingIds.ToList();
        if (!listings.Contains(listingId))
        {
            listings.Add(listingId);
        }
        return this with { SelectedListingIds = listings, Version = Version + 1 };
    }
}

public record LocationCriteria(string Province, string Locality, string? Neighborhood);
public record FinancialCriteria(Money MinBudget, Money MaxBudget);
public record Money(decimal Amount, string Currency);
public record CommercialProgress(string StageCode, string StageCatalogVersion, DateTimeOffset ChangedAt, string? ChangedBy);
