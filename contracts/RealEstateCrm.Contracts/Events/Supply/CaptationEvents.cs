using System;
using System.Collections.Generic;
using RealEstateCrm.Contracts.Events.Demand;
using RealEstateCrm.Contracts.Context;

namespace RealEstateCrm.Contracts.Events.Supply;

public record CaptationCaseOpenedV1(
    string CaptationCaseId,
    List<Guid> ContactPartyIds,
    string? CandidatePropertyId,
    string? ResponsibleUserId,
    string? OriginCode,
    string Status,
    CommercialProgressDto CommercialProgress,
    long Version);

public record CaptationCapturedV1(
    string CaptationCaseId,
    long Version);

public record CaptationLostV1(
    string CaptationCaseId,
    long Version);
