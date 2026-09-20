using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using RealEstateCrm.BuildingBlocks.References;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.References.Listings;

public class HttpListingReferencePort(HttpClient httpClient) : IListingReferencePort
{
    public async Task<ListingReferenceV1?> GetListingAsync(string listingId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/v1/listings/{listingId}/reference", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ListingReferenceV1>(cancellationToken: cancellationToken);
    }
}
