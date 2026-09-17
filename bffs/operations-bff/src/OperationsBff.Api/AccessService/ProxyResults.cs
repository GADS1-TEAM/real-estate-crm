using Microsoft.AspNetCore.Mvc;

namespace OperationsBff.Api.AccessService;

/// <summary>
/// El BFF reenvía la respuesta de access-service tal cual (mismo status code, mismo body
/// <c>ProblemDetailsV1</c> en error): no la reinterpreta ni la remapea, para no perder el
/// <c>errorCode</c>/403 que ya armó el backend (D4).
/// </summary>
internal static class ProxyResults
{
    public static async Task<IActionResult> FromAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
        };
    }
}
