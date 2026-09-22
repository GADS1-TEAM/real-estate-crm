using MongoDB.Driver;
using DemandService.Domain.Aggregates;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

namespace DemandService.Infrastructure.Persistence.Mongo;

public class RequirementRepository : VersionedMongoRepository<Requirement, string>
{
    static RequirementRepository()
    {
        if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(Requirement)))
        {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Requirement>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(r => r.RequirementId);
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    public RequirementRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor)
        : base(database, "requirements", sessionAccessor, r => r.RequirementId, r => (int)r.Version)
    {
    }
}
