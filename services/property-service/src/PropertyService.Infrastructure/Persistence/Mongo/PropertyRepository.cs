using MongoDB.Driver;
using PropertyService.Domain.Aggregates;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyService.Infrastructure.Persistence.Mongo;

public class PropertyRepository : VersionedMongoRepository<Property, string>
{
    public PropertyRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor) 
        : base(database, "Properties", sessionAccessor, p => p.PropertyId, p => p.Version)
    {
    }
}