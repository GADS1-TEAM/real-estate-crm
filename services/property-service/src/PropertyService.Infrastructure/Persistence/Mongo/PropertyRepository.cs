using MongoDB.Driver;
using PropertyService.Domain.Aggregates;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyService.Infrastructure.Persistence.Mongo;

public class PropertyRepository : VersionedMongoRepository<Property, string>
{
    static PropertyRepository()
    {
        if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(Property)))
        {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Property>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(p => p.PropertyId);
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    public PropertyRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor) 
        : base(database, "Properties", sessionAccessor, p => p.PropertyId, p => p.Version)
    {
    }
}