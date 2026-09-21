using System;
using System.Collections.Generic;

namespace AutomationAiService.Domain;

public class AgentDecision
{
    public Guid DecisionId { get; private set; }
    public Guid PipelineItemId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string AgentType { get; private set; }
    public string ModelRef { get; private set; }
    public IReadOnlyList<string> InputEvidenceRefs { get; private set; }
    public string Summary { get; private set; }
    public string SuggestedPriority { get; private set; }
    public string MissingInformation { get; private set; }
    public double Confidence { get; private set; }
    public string PolicyVersion { get; private set; }
    public string ReviewStatus { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public AgentDecision(
        Guid decisionId,
        Guid pipelineItemId,
        Guid requestedByUserId,
        string agentType,
        string modelRef,
        IReadOnlyList<string> inputEvidenceRefs,
        string summary,
        string suggestedPriority,
        string missingInformation,
        double confidence,
        string policyVersion,
        string reviewStatus,
        DateTimeOffset createdAt)
    {
        DecisionId = decisionId;
        PipelineItemId = pipelineItemId;
        RequestedByUserId = requestedByUserId;
        AgentType = agentType;
        ModelRef = modelRef;
        InputEvidenceRefs = inputEvidenceRefs;
        Summary = summary;
        SuggestedPriority = suggestedPriority;
        MissingInformation = missingInformation;
        Confidence = confidence;
        PolicyVersion = policyVersion;
        ReviewStatus = reviewStatus;
        CreatedAt = createdAt;
    }

    public void Review(string status, Guid reviewedByUserId, DateTimeOffset reviewedAt)
    {
        if (ReviewStatus != "PROPOSED")
        {
            throw new InvalidOperationException("Decision is already reviewed");
        }

        ReviewStatus = status;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAt;
    }
}
