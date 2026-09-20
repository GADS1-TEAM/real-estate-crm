using MongoDB.Bson.Serialization;
using SupplyService.Domain.Aggregates;
using RealEstateCrm.Contracts.Financial;

namespace SupplyService.Infrastructure.Mongo;

public static class MongoClassMappings
{
    public static void RegisterMappings()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Listing)))
        {
            BsonClassMap.RegisterClassMap<Listing>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(l => l.ListingId);
            });
        }
        
        if (!BsonClassMap.IsClassMapRegistered(typeof(ListingCommercialTerms)))
        {
            BsonClassMap.RegisterClassMap<ListingCommercialTerms>(cm => cm.AutoMap());
        }
        
        if (!BsonClassMap.IsClassMapRegistered(typeof(ListingAvailability)))
        {
            BsonClassMap.RegisterClassMap<ListingAvailability>(cm => cm.AutoMap());
        }
    }
}
