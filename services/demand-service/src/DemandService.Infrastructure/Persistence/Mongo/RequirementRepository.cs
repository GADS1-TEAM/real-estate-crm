using MongoDB.Driver;
using DemandService.Domain.Aggregates;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

namespace DemandService.Infrastructure.Persistence.Mongo;

public class RequirementRepository : VersionedMongoRepository<Requirement, string>
{
    public RequirementRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
        : base(database, "requirements", sessionAccessor, r => r.RequirementId, r => (int)r.Version)
    {
    }
}
