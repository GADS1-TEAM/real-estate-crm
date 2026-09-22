using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using RealEstateCrm.Contracts.Serialization;

namespace OperationsBff.Api.AutomationAiService;

/// <summary>
/// Cliente HTTP tipado hacia automation-ai-service, con token relay (D6).
/// </summary>
public sealed class AutomationAiServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
{
    public async Task<HttpResponseMessage> GetAsync(string path, CancellationToken cancellationToken) =>
        await SendAsync(HttpMethod.Get, path, body: null, cancellationToken);

    public async Task<HttpResponseMessage> PostAsync(string path, object? body, CancellationToken cancellationToken) =>
        await SendAsync(HttpMethod.Post, path, body, cancellationToken);

    public async Task<HttpResponseMessage> PutAsync(string path, object? body, CancellationToken cancellationToken) =>
        await SendAsync(HttpMethod.Put, path, body, cancellationToken);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: RealEstateCrmJsonDefaults.Options);
        }

        var accessToken = httpContextAccessor.HttpContext is { } httpContext
            ? await httpContext.GetTokenAsync("access_token")
            : null;

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await httpClient.SendAsync(request, cancellationToken);
    }
}
