using System;
using System.Collections.Generic;

namespace SupplyService.Domain.Aggregates;

public record CaptationCase(
    string CaptationCaseId,
    List<Guid> ContactPartyIds,
    string? CandidatePropertyId,
    string? ResponsibleUserId,
    string? OriginCode,
    string Status,
    List<string> ValuationRefs,
    string? MandateRef,
    CommercialProgress CommercialProgress,
    long Version)
{
    public static CaptationCase Open(string captationCaseId, List<Guid> contactPartyIds, string? candidatePropertyId, string? responsibleUserId, string? originCode)
    {
        return new CaptationCase(
            captationCaseId,
            contactPartyIds,
            candidatePropertyId,
            responsibleUserId,
            originCode,
            "IDENTIFIED",
            new List<string>(),
            null,
            new CommercialProgress("SUPPLY_START", "1.0", DateTimeOffset.UtcNow, null),
            1);
    }

    public CaptationCase UpdateStatus(string status)
    {
        return this with { Status = status, Version = Version + 1 };
    }
}

public record CommercialProgress(string StageCode, string StageCatalogVersion, DateTimeOffset ChangedAt, string? ChangedBy);
