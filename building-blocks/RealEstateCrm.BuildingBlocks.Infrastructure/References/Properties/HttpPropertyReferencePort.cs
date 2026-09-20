using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using RealEstateCrm.BuildingBlocks.References;
using RealEstateCrm.Contracts.Properties;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.References.Properties;

public class HttpPropertyReferencePort(HttpClient httpClient) : IPropertyReferencePort
{
    public async Task<PropertyReferenceV1?> GetPropertyAsync(string propertyId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/v1/properties/{propertyId}/reference", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PropertyReferenceV1>(cancellationToken: cancellationToken);
    }
}
