using System;
using System.Collections.Generic;

namespace RealEstateCrm.Contracts.Opportunities;

public record CommercialPipelineItemV1(
    string PipelineItemId,
    string SourceType,
    string SourceId,
    string PipelineKind,
    string Title,
    List<Guid> RelatedPartyIds,
    string? PropertyId,
    string? ListingId,
    string? OperationTypeCode,
    string? ResponsibleUserId,
    string? OriginCode,
    decimal? EstimatedValue,
    DateTimeOffset? EstimatedCloseDate,
    string CurrentStageCode,
    string SemanticStatus,
    List<StageHistoryItemV1> StageHistory,
    long Version
);

public record StageHistoryItemV1(
    string StageCode,
    DateTimeOffset EnteredAt,
    string? EnteredBy
);

public record CreateOpportunityRequestV1(
    string PipelineKind,
    string Title,
    string OperationTypeCode,
    List<Guid> RelatedPartyIds,
    string? ResponsibleUserId
);

public record UpdateOpportunityRequestV1(
    string Title,
    decimal? EstimatedValue,
    DateTimeOffset? EstimatedCloseDate,
    string? ResponsibleUserId
);

public record ChangeOpportunityStageRequestV1(
    string NewStageCode
);

public record CloseOpportunityWinRequestV1(
    decimal FinalValue,
    DateTimeOffset ClosedDate
);

public record CloseOpportunityLossRequestV1(
    string LossReason,
    DateTimeOffset ClosedDate
);
