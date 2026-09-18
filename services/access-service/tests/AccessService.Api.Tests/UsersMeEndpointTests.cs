using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Serialization;
using RealEstateCrm.Contracts.Users;

namespace AccessService.Api.Tests;

/// <summary>
/// <c>GET /api/v1/users/me</c> (V2-PTY-001) contra Mongo, RabbitMQ y Keycloak reales, con el seed
/// de desarrollo (D9): el <c>userId</c> devuelto es el de access-service, distinto del <c>sub</c>
/// fijo del realm, y el rol sale de la asignación real.
/// </summary>
[Trait("Category", "RequiresMongo")]
[Trait("Category", "RequiresRabbitMq")]
[Trait("Category", "RequiresKeycloak")]
public class UsersMeEndpointTests : IAsyncLifetime
{
    private static readonly string KeycloakBaseUrl = Environment.GetEnvironmentVariable("KEYCLOAK_BASE_URL") ?? "http://localhost:8080";
    private static readonly string KeycloakRealm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? "crm-dev";
    private static readonly string KeycloakClientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "operations-bff";

    private static readonly Guid VendedorSubject = Guid.Parse("a0000000-0000-4000-8000-000000000002");

    private readonly string _mongoDatabaseName = "pty001_me_" + Guid.NewGuid().ToString("N");
    private readonly string _rabbitMqExchangeName = "pty001-me-exchange-" + Guid.NewGuid().ToString("N")[..8];

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // UseSetting (no ConfigureAppConfiguration): AddMongoPersistence lee Mongo:DatabaseName de forma eager al armar Program.cs.
            builder.UseSetting("Mongo:DatabaseName", _mongoDatabaseName);
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Mongo:DatabaseName"] = _mongoDatabaseName,
                    ["RabbitMq:ServiceExchangeName"] = _rabbitMqExchangeName,
                    ["OutboxRelay:PollingInterval"] = "00:00:00.200",
                });
            });
        });

        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();

        using var mongoClient = new MongoDB.Driver.MongoClient(
            Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017/?directConnection=true");
        await mongoClient.DropDatabaseAsync(_mongoDatabaseName);
    }

    private static async Task<string> GetAccessTokenAsync(string username)
    {
        using var client = new HttpClient { BaseAddress = new Uri(KeycloakBaseUrl) };

        var response = await client.PostAsync(
            $"/realms/{KeycloakRealm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = KeycloakClientId,
                ["username"] = username,
                ["password"] = username,
            }));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return payload!["access_token"].ToString()!;
    }

    private async Task<HttpResponseMessage> GetMeAsync(string? username)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");

        if (username is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(username));
        }

        return await _client.SendAsync(request);
    }

    [Theory]
    [InlineData("dev.administrador", "Administrador")]
    [InlineData("dev.vendedor", "Vendedor")]
    [InlineData("dev.responsable", "Responsable Comercial")]
    public async Task Each_seeded_user_resolves_to_their_own_userId_and_role(string username, string expectedRole)
    {
        var response = await GetMeAsync(username);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var self = (await response.Content.ReadFromJsonAsync<UserSelfV1>(RealEstateCrmJsonDefaults.Options))!;
        Assert.Equal("Active", self.Status);
        Assert.Equal(expectedRole, self.RoleCode);
        Assert.NotEqual(Guid.Empty, self.UserId);
    }

    [Fact]
    public async Task The_returned_userId_is_the_access_service_id_not_the_keycloak_subject()
    {
        var self = (await (await GetMeAsync("dev.vendedor")).Content.ReadFromJsonAsync<UserSelfV1>(RealEstateCrmJsonDefaults.Options))!;

        Assert.NotEqual(VendedorSubject, self.UserId);
    }

    [Fact]
    public async Task The_same_user_resolves_to_the_same_userId_every_time()
    {
        var first = (await (await GetMeAsync("dev.vendedor")).Content.ReadFromJsonAsync<UserSelfV1>(RealEstateCrmJsonDefaults.Options))!;
        var second = (await (await GetMeAsync("dev.vendedor")).Content.ReadFromJsonAsync<UserSelfV1>(RealEstateCrmJsonDefaults.Options))!;

        Assert.Equal(first.UserId, second.UserId);
    }

    [Fact]
    public async Task Without_a_token_it_is_a_401()
    {
        var response = await GetMeAsync(username: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_login_is_auto_provisioned_PENDING_and_gets_a_403_user_pending()
    {
        // D3: el primer login de un sub desconocido crea un UserAccount PENDING; /me lo rechaza con el reasonCode de ACL.
        // Se borra el seed del vendedor de ESTA base efímera para reproducir un sub sin cuenta.
        using var mongoClient = new MongoDB.Driver.MongoClient(
            Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017/?directConnection=true");
        var database = mongoClient.GetDatabase(_mongoDatabaseName);
        await GetMeAsync("dev.administrador"); // fuerza el arranque del host (y del seed) antes de tocar la base.
        await database.GetCollection<MongoDB.Bson.BsonDocument>("user_accounts").DeleteOneAsync(
            MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("KeycloakSubject", new MongoDB.Bson.BsonBinaryData(VendedorSubject, MongoDB.Bson.GuidRepresentation.Standard)));

        var response = await GetMeAsync("dev.vendedor");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetailsV1>(RealEstateCrmJsonDefaults.Options))!;
        Assert.Equal(ErrorCodes.Forbidden, problem.ErrorCode);
        Assert.Equal(DenyReasons.UserPending, problem.Detail);
    }
}
