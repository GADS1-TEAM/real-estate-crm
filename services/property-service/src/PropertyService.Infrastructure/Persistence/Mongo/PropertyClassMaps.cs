using MongoDB.Bson.Serialization;
using PropertyService.Domain.Aggregates;

namespace PropertyService.Infrastructure.Persistence.Mongo;

public static class PropertyClassMaps
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<Property>(cm =>
        {
            cm.AutoMap();
            cm.MapIdProperty(p => p.PropertyId);
        });

        BsonClassMap.RegisterClassMap<PropertyInterest>(cm =>
        {
            cm.AutoMap();
            cm.MapIdProperty(p => p.InterestId);
        });
    }
}
