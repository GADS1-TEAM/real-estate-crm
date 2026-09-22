using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using SupplyService.Application.Ports;
using SupplyService.Domain.Aggregates;
using SupplyService.Domain.Exceptions;

namespace SupplyService.Infrastructure.Mongo;

public class VersionedMongoRepository : IListingRepository
{
    private readonly IMongoCollection<Listing> _collection;

    static VersionedMongoRepository()
    {
        if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(Listing)))
        {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Listing>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.ListingId);
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    public VersionedMongoRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Listing>("Listings");
    }

    public async Task<Listing?> GetByIdAsync(string listingId, CancellationToken cancellationToken)
    {
        var cursor = await _collection.FindAsync(x => x.ListingId == listingId, null, cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(Listing listing, CancellationToken cancellationToken)
    {
        if (listing.Version == 1)
        {
            await _collection.InsertOneAsync(listing, new InsertOneOptions(), cancellationToken);
        }
        else
        {
            var filter = Builders<Listing>.Filter.Eq(x => x.ListingId, listing.ListingId) &
                         Builders<Listing>.Filter.Eq(x => x.Version, listing.Version - 1);
            
            var result = await _collection.ReplaceOneAsync(filter, listing, new ReplaceOptions { IsUpsert = false }, cancellationToken);
            
            if (result.ModifiedCount == 0)
            {
                throw new DomainException("concurrency_conflict", "Concurrency conflict occurred during save.");
            }
        }
    }
}
