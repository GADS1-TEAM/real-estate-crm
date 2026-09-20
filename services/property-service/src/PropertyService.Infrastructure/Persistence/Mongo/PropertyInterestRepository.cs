using MongoDB.Driver;
using PropertyService.Domain.Aggregates;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyService.Infrastructure.Persistence.Mongo;

public class PropertyInterestRepository : MongoRepository<PropertyInterest, string>
{
    public PropertyInterestRepository(IMongoDatabase database, MongoSessionAccessor sessionAccessor) 
        : base(database, "PropertyInterests", sessionAccessor, p => p.InterestId)
    {
    }
}