using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutomationAiService.Domain;

public interface IAgentDecisionRepository
{
    Task SaveAsync(AgentDecision decision, CancellationToken cancellationToken = default);
    Task<AgentDecision?> GetByIdAsync(Guid decisionId, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentDecision decision, CancellationToken cancellationToken = default);
}
