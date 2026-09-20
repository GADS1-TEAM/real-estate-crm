using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RealEstateCrm.BuildingBlocks.References;

namespace RealEstateCrm.TestSupport.Fakes;

public class FakeListingReferencePort : IListingReferencePort
{
    private readonly ConcurrentDictionary<string, ListingReferenceV1> _listings = new();

    public void AddListing(ListingReferenceV1 listing) => _listings[listing.ListingId] = listing;

    public Task<ListingReferenceV1?> GetListingAsync(string listingId, CancellationToken cancellationToken = default)
    {
        _listings.TryGetValue(listingId, out var listing);
        return Task.FromResult(listing);
    }
}
