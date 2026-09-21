namespace RealEstateCrm.Contracts.AI;

public record AgentDecisionV1(
    Guid DecisionId,
    Guid PipelineItemId,
    Guid RequestedByUserId,
    string AgentType,
    string ModelRef,
    IReadOnlyList<string> InputEvidenceRefs,
    string Summary,
    string SuggestedPriority,
    string MissingInformation,
    double Confidence,
    string PolicyVersion,
    string ReviewStatus,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt
);

public record AnalyzeOpportunityRequestV1(); // Not specified with fields, but used in POST /api/v1/ai/opportunities/{pipelineItemId}/analyze ? Wait, if it is in the URL, maybe it doesn't need to be in the body?
// Let's just define it as empty or with PipelineItemId?
// The instructions: "AnalyzeOpportunityRequestV1, ReviewDecisionRequestV1"

public record ReviewDecisionRequestV1(
    string ReviewStatus,
    Guid ReviewedByUserId
);
