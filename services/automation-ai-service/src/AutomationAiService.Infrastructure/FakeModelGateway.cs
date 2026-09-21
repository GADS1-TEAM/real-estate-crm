using System;
using System.Threading;
using System.Threading.Tasks;
using AutomationAiService.Domain;

namespace AutomationAiService.Infrastructure;

public class FakeModelGateway : IModelGateway
{
    public Task<ModelAnalysisResult> AnalyzeOpportunityAsync(Guid pipelineItemId, object context, CancellationToken cancellationToken = default)
    {
        var result = new ModelAnalysisResult(
            ModelRef: "gpt-4o-fake",
            InputEvidenceRefs: new[] { "prop-docs/1", "req-docs/2" },
            Summary: "Highly interested prospect",
            SuggestedPriority: "HIGH",
            MissingInformation: "Budget confirmation",
            Confidence: 0.92,
            PolicyVersion: "v1.2"
        );
        return Task.FromResult(result);
    }
}
