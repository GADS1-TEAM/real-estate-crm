using System.Net.Http;
using System.Threading.Tasks;
using RealEstateCrm.BuildingBlocks.References;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Http;

public class HttpCaptationReferencePort : ICaptationReferencePort
{
    private readonly HttpClient _httpClient;

    public HttpCaptationReferencePort(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ExistsAsync(string captationCaseId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/captations/{captationCaseId}");
        return response.IsSuccessStatusCode;
    }
}
