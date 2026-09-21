using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutomationAiService.Domain;

public record ModelAnalysisResult(
    string ModelRef,
    IReadOnlyList<string> InputEvidenceRefs,
    string Summary,
    string SuggestedPriority,
    string MissingInformation,
    double Confidence,
    string PolicyVersion
);

public interface IModelGateway
{
    Task<ModelAnalysisResult> AnalyzeOpportunityAsync(Guid pipelineItemId, object context, CancellationToken cancellationToken = default);
}
