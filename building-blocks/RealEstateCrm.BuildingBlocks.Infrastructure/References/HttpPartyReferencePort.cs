using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RealEstateCrm.BuildingBlocks.References;
using RealEstateCrm.BuildingBlocks.Infrastructure.Http;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.References;

internal sealed class HttpPartyReferencePort : IPartyReferencePort
{
    private readonly HttpClient _httpClient;

    public HttpPartyReferencePort(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PartyReferenceV1?> GetAsync(Guid partyId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/parties/{partyId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var party = await response.Content.ReadFromJsonAsync<PartyReferenceV1>(cancellationToken: ct);
        return party;
    }
}

public static class PartyReferenceServiceCollectionExtensions
{
    public static IServiceCollection AddPartyReferenceHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["PartyService:BaseUrl"] ?? throw new InvalidOperationException("PartyService:BaseUrl is missing");
        
        services.AddHttpClient<IPartyReferencePort, HttpPartyReferencePort>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<BearerTokenRelayHandler>();

        return services;
    }
}
