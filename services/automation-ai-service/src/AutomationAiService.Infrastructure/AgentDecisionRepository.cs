using System;
using System.Threading;
using System.Threading.Tasks;
using AutomationAiService.Domain;
using MongoDB.Driver;

namespace AutomationAiService.Infrastructure;

public class AgentDecisionRepository : IAgentDecisionRepository
{
    private readonly IMongoCollection<AgentDecision> _collection;

    public AgentDecisionRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<AgentDecision>("crm_automation_ai");
    }

    public async Task SaveAsync(AgentDecision decision, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(decision, new InsertOneOptions(), cancellationToken);
    }

    public async Task<AgentDecision?> GetByIdAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(d => d.DecisionId == decisionId, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(AgentDecision decision, CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(d => d.DecisionId == decision.DecisionId, decision, new ReplaceOptions(), cancellationToken);
    }
}
