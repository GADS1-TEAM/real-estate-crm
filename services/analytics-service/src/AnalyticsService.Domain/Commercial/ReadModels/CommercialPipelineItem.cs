using System;
using System.Collections.Generic;

namespace AnalyticsService.Domain.Commercial.ReadModels;

public class CommercialPipelineItem
{
    public string Id { get; set; } = string.Empty;
    public string PipelineItemId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string PipelineKind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<Guid> RelatedPartyIds { get; set; } = new();
    public string? PropertyId { get; set; }
    public string? ListingId { get; set; }
    public string? OperationTypeCode { get; set; }
    public string? ResponsibleUserId { get; set; }
    public string? OriginCode { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateTimeOffset? EstimatedCloseDate { get; set; }
    public string CurrentStageCode { get; set; } = string.Empty;
    public string SemanticStatus { get; set; } = string.Empty;
    public List<StageHistoryItem> StageHistory { get; set; } = new();
    public long Version { get; set; }
}

public class StageHistoryItem
{
    public string StageCode { get; set; } = string.Empty;
    public DateTimeOffset EnteredAt { get; set; }
    public string? EnteredBy { get; set; }
}
