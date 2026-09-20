using System.Net.Http;
using System.Threading.Tasks;
using RealEstateCrm.BuildingBlocks.References;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Http;

public class HttpRequirementReferencePort : IRequirementReferencePort
{
    private readonly HttpClient _httpClient;

    public HttpRequirementReferencePort(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ExistsAsync(string requirementId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/requirements/{requirementId}");
        return response.IsSuccessStatusCode;
    }
}
