using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace PlatformAdminBff.Api.Admin;

[ApiController]
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AdminController(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    [HttpGet("health")]
    [HttpGet("/health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "PlatformAdminBff",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "Online",
            platform = "Real Estate CRM - Platform Administration",
            version = "1.0.0",
            services = new[]
            {
                new { name = "AccessService", url = _configuration["AccessService:BaseUrl"] ?? "http://localhost:5050" },
                new { name = "PlatformConfigService", url = _configuration["PlatformConfigService:BaseUrl"] ?? "http://localhost:5200" }
            }
        });
    }

    [HttpGet("catalogs")]
    public async Task<IActionResult> GetCatalogs(CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["PlatformConfigService:BaseUrl"] ?? "http://localhost:5200";
        try
        {
            var response = await _httpClient.GetAsync($"{baseUrl}/api/v1/catalogs", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return Content(content, "application/json");
            }
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "PlatformConfigService unavailable", details = ex.Message });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["AccessService:BaseUrl"] ?? "http://localhost:5050";
        try
        {
            var response = await _httpClient.GetAsync($"{baseUrl}/api/v1/users", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return Content(content, "application/json");
            }
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AccessService unavailable", details = ex.Message });
        }
    }
}
