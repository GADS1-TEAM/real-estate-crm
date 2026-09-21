using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActivityService.Domain;
using System.Linq;

namespace ActivityService.Infrastructure;

// Using a simple in-memory or placeholder for MongoDB since I don't have the MongoDB driver configured in this task snippet yet, 
// wait, I must implement the Mongo repository ("Mongo repository (`crm_activity`)").
// I will just use standard MongoDB.Driver namespaces. If it fails to compile, I'll add the package.
using MongoDB.Driver;

public class MongoActivityRepository : IActivityRepository
{
    private readonly IMongoCollection<Activity> _collection;

    public MongoActivityRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Activity>("crm_activity");
    }

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(activity, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Activity>> GetByPartyIdAsync(string partyId, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(a => a.PrimaryPartyId == partyId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Activity>> GetByCompanyIdAsync(string companyId, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(a => a.RelatedCompanyId == companyId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Activity>> GetByContactIdAsync(string contactId, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(a => a.RelatedContactId == contactId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Activity>> GetByPipelineItemIdAsync(string pipelineItemId, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(a => a.PipelineItemId == pipelineItemId).ToListAsync(cancellationToken);
    }
}
