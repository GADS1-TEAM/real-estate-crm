using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutomationAiService.Domain;
using RealEstateCrm.Contracts.AI;

namespace AutomationAiService.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
public class AiAssistantController : ControllerBase
{
    private readonly IModelGateway _modelGateway;
    private readonly IAgentDecisionRepository _repository;

    public AiAssistantController(IModelGateway modelGateway, IAgentDecisionRepository repository)
    {
        _modelGateway = modelGateway;
        _repository = repository;
    }

    [HttpPost("opportunities/{pipelineItemId}/analyze")]
    public async Task<IActionResult> AnalyzeOpportunity(Guid pipelineItemId, [FromBody] AnalyzeOpportunityRequestV1 request, CancellationToken cancellationToken)
    {
        var context = new { /* Fetch context in a real implementation */ };
        var analysis = await _modelGateway.AnalyzeOpportunityAsync(pipelineItemId, context, cancellationToken);
        
        var decision = new AgentDecision(
            decisionId: Guid.NewGuid(),
            pipelineItemId: pipelineItemId,
            requestedByUserId: Guid.NewGuid(), // Normally from auth context
            agentType: "COMMERCIAL_ASSISTANT",
            modelRef: analysis.ModelRef,
            inputEvidenceRefs: analysis.InputEvidenceRefs,
            summary: analysis.Summary,
            suggestedPriority: analysis.SuggestedPriority,
            missingInformation: analysis.MissingInformation,
            confidence: analysis.Confidence,
            policyVersion: analysis.PolicyVersion,
            reviewStatus: "PROPOSED",
            createdAt: DateTimeOffset.UtcNow
        );

        await _repository.SaveAsync(decision, cancellationToken);

        var dto = new AgentDecisionV1(
            decision.DecisionId,
            decision.PipelineItemId,
            decision.RequestedByUserId,
            decision.AgentType,
            decision.ModelRef,
            decision.InputEvidenceRefs,
            decision.Summary,
            decision.SuggestedPriority,
            decision.MissingInformation,
            decision.Confidence,
            decision.PolicyVersion,
            decision.ReviewStatus,
            decision.ReviewedByUserId,
            decision.ReviewedAt,
            decision.CreatedAt
        );

        return Ok(dto);
    }

    [HttpPost("decisions/{decisionId}/review")]
    public async Task<IActionResult> ReviewDecision(Guid decisionId, [FromBody] ReviewDecisionRequestV1 request, CancellationToken cancellationToken)
    {
        var decision = await _repository.GetByIdAsync(decisionId, cancellationToken);
        if (decision == null)
            return NotFound();

        decision.Review(request.ReviewStatus, request.ReviewedByUserId, DateTimeOffset.UtcNow);

        await _repository.UpdateAsync(decision, cancellationToken);

        return NoContent();
    }
}
