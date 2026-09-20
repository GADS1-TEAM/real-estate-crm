using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OperationsBff.Api.PropertyService;

public class PropertyServiceClient(HttpClient httpClient)
{
    public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
        => httpClient.GetAsync(requestUri, cancellationToken);
    
    public Task<HttpResponseMessage> PostAsync(string requestUri, object? value, CancellationToken cancellationToken)
        => httpClient.PostAsJsonAsync(requestUri, value, cancellationToken);

    public Task<HttpResponseMessage> PutAsync(string requestUri, object? value, CancellationToken cancellationToken)
        => httpClient.PutAsJsonAsync(requestUri, value, cancellationToken);
}
