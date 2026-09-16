using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using RealEstateCrm.BuildingBlocks.Authentication;
using RealEstateCrm.BuildingBlocks.Infrastructure.Authentication;
using RealEstateCrm.Contracts.Errors;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Authentication;

/// <summary>
/// AUTH-001/AUTH-002 happy y error path, sin depender de un Keycloak real: firma los tokens
/// con una clave simétrica de test y valida contra ella. El test que sí necesita Keycloak
/// corriendo está separado y marcado (ver <see cref="KeycloakDevRealmTests"/>).
/// </summary>
public class JwtBearerAuthenticationPipelineTests
{
    private const string Issuer = "https://keycloak-dev.local/realms/crm-dev";
    private const string Audience = "operations-bff";
    private static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes("test-signing-key-at-least-32-bytes-long!!"));

    [Fact]
    public async Task Request_without_token_returns_stable_problem_details_401()
    {
        using var host = await CreateHostAsync();
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/secure");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsV1>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.Unauthorized, problem.ErrorCode);
        Assert.Equal(401, problem.Status);
    }

    [Fact]
    public async Task Request_with_expired_token_returns_stable_problem_details_401()
    {
        using var host = await CreateHostAsync();
        using var client = host.GetTestClient();

        var expiredToken = CreateToken(Guid.NewGuid(), "Dev User", "dev@local.test", ["Vendedor"], expired: true);
        client.DefaultRequestHeaders.Authorization = new("Bearer", expiredToken);

        var response = await client.GetAsync("/secure");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsV1>();
        Assert.Equal(ErrorCodes.Unauthorized, problem!.ErrorCode);
    }

    [Fact]
    public async Task Request_with_valid_token_resolves_authenticated_user_with_roles()
    {
        using var host = await CreateHostAsync();
        using var client = host.GetTestClient();

        var userId = Guid.NewGuid();
        var token = CreateToken(userId, "Ana Vendedora", "ana@demo.test", ["Vendedor", "ResponsableComercial"], expired: false);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync("/secure");

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<AuthenticatedUser>();
        Assert.NotNull(user);
        Assert.Equal(userId, user.UserId);
        Assert.Equal("Ana Vendedora", user.DisplayName);
        Assert.Equal("ana@demo.test", user.Email);
        Assert.Contains("Vendedor", user.Roles);
        Assert.Contains("ResponsableComercial", user.Roles);
    }

    private static async Task<IHost> CreateHostAsync()
    {
        var hostBuilder = new HostBuilder().ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddKeycloakJwtBearerAuthentication(bearerOptions =>
                    {
                        bearerOptions.RequireHttpsMetadata = false;
                        bearerOptions.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = Issuer,
                            ValidateAudience = true,
                            ValidAudience = Audience,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = SigningKey,
                            ValidateLifetime = true,
                            NameClaimType = "preferred_username",
                        };
                    });
                    services.AddAuthorization();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/secure", async context =>
                        {
                            var authPort = context.RequestServices.GetRequiredService<IAuthenticationPort>();
                            var user = await authPort.ResolveCurrentUserAsync(context.RequestAborted);
                            await context.Response.WriteAsJsonAsync(user);
                        }).RequireAuthorization();
                    });
                });
        });

        return await hostBuilder.StartAsync();
    }

    private static string CreateToken(Guid userId, string displayName, string email, IReadOnlyCollection<string> roles, bool expired)
    {
        var realmAccessJson = $$"""{"roles":[{{string.Join(",", roles.Select(r => $"\"{r}\""))}}]}""";

        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("name", displayName),
            new("email", email),
            new("realm_access", realmAccessJson, JsonClaimValueTypes.Json),
        };

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: expired ? now.AddHours(-2) : now,
            expires: expired ? now.AddHours(-1) : now.AddHours(1),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
