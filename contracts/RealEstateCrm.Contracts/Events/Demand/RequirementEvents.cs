using System;
using System.Collections.Generic;
using RealEstateCrm.Contracts.Financial;
using RealEstateCrm.Contracts.Context;

namespace RealEstateCrm.Contracts.Events.Demand;

public record RequirementCreatedV1(
    string RequirementId,
    List<Guid> SeekerPartyIds,
    string OperationTypeCode,
    string? PropertyTypeCode,
    LocationCriteriaDto? LocationCriteria,
    MoneyV1? MinBudget,
    MoneyV1? MaxBudget,
    List<string> SelectedListingIds,
    string? ResponsibleUserId,
    string? OriginCode,
    CommercialProgressDto CommercialProgress,
    string Status,
    long Version);

public record RequirementUpdatedV1(
    string RequirementId,
    string Status,
    long Version);

public record RequirementActivatedV1(
    string RequirementId,
    List<Guid> SeekerPartyIds,
    string OperationTypeCode,
    string? PropertyTypeCode,
    LocationCriteriaDto? LocationCriteria,
    MoneyV1? MinBudget,
    MoneyV1? MaxBudget,
    long Version);

public record LocationCriteriaDto(string Province, string Locality, string? Neighborhood);
public record CommercialProgressDto(string StageCode, string StageCatalogVersion, DateTimeOffset ChangedAt, string? ChangedBy);
