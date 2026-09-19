using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.Contracts.Catalogs;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Serialization;
using RealEstateCrm.TestSupport.Authorization;

namespace PlatformConfigService.Api.Tests;

/// <summary>
/// Contra infraestructura real: Mongo (persistencia) y RabbitMQ (outbox relay) reales, más un
/// JWT real emitido por el Keycloak de <c>docker-compose.yml</c> (para que <c>[Authorize]</c>
/// deje pasar la request). La DECISIÓN de <c>IAuthorizationPort</c> se sustituye por
/// <c>FakeAuthorizationPort</c> (D2, mismo double que documenta
/// <c>IMPLEMENTATION_REPORT-V2-ACL-001a.md</c> para los tests de CAT-001/PTY-001): el
/// <c>HttpAuthorizationPort</c> real que usa <c>Program.cs</c> en producción necesita
/// access-service corriendo como proceso aparte (D10, fuera de Compose), algo que
/// <c>scripts/test-integration.sh</c> no levanta. Sustituirlo acá prueba el mapeo
/// decisión→403/200 de <c>CatalogsController</c> sin acoplar este test a que otro servicio esté
/// vivo.
/// </summary>
/// <remarks>
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresMongo&amp;Category!=RequiresRabbitMq&amp;Category!=RequiresKeycloak"</c>.
/// </remarks>
[Trait("Category", "RequiresMongo")]
[Trait("Category", "RequiresRabbitMq")]
[Trait("Category", "RequiresKeycloak")]
public class CatalogsAuthorizationEndToEndTests
{
    private static readonly string KeycloakBaseUrl = Environment.GetEnvironmentVariable("KEYCLOAK_BASE_URL") ?? "http://localhost:8080";
    private static readonly string KeycloakRealm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? "crm-dev";
    private static readonly string KeycloakClientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "operations-bff";

    private static WebApplicationFactory<Program> NewFactory(string mongoDatabaseName, string rabbitMqExchangeName, IAuthorizationPort authorizationPort) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // UseSetting (no ConfigureAppConfiguration): AddMongoPersistence lee Mongo:DatabaseName de forma eager al armar Program.cs.
            builder.UseSetting("Mongo:DatabaseName", mongoDatabaseName);
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Mongo:DatabaseName"] = mongoDatabaseName,
                    ["RabbitMq:ServiceExchangeName"] = rabbitMqExchangeName,
                    ["OutboxRelay:PollingInterval"] = "00:00:00.200",
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthorizationPort>();
                services.AddSingleton(authorizationPort);
            });
        });

    private static async Task DropMongoDatabaseAsync(string databaseName)
    {
        using var mongoClient = new MongoDB.Driver.MongoClient(
            Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017/?directConnection=true");
        await mongoClient.DropDatabaseAsync(databaseName);
    }

    private static async Task<string> GetAccessTokenAsync(string username, string password)
    {
        using var client = new HttpClient { BaseAddress = new Uri(KeycloakBaseUrl) };

        var response = await client.PostAsync(
            $"/realms/{KeycloakRealm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = KeycloakClientId,
                ["username"] = username,
                ["password"] = password,
            }));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return payload!["access_token"].ToString()!;
    }

    [Fact]
    public async Task A_denied_decision_is_rejected_with_a_403_problem_details()
    {
        var mongoDatabaseName = "cat001_e2e_" + Guid.NewGuid().ToString("N");
        var rabbitMqExchangeName = "cat001-e2e-exchange-" + Guid.NewGuid().ToString("N")[..8];

        await using var factory = NewFactory(mongoDatabaseName, rabbitMqExchangeName, FakeAuthorizationPort.DenyAll());
        using var client = factory.CreateClient();

        var token = await GetAccessTokenAsync("dev.vendedor", "dev.vendedor");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/v1/catalogs",
            new { catalogType = CatalogTypes.LossReason, code = "NO_DEBERIA_CREARSE", label = "No debería crearse", order = (int?)null, pipelineKind = (string?)null, semanticState = (string?)null },
            RealEstateCrmJsonDefaults.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsV1>(RealEstateCrmJsonDefaults.Options);
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.Forbidden, problem!.ErrorCode);

        await DropMongoDatabaseAsync(mongoDatabaseName);
    }

    [Fact]
    public async Task An_allowed_actor_can_create_an_entry_and_read_it_back()
    {
        var mongoDatabaseName = "cat001_e2e_" + Guid.NewGuid().ToString("N");
        var rabbitMqExchangeName = "cat001-e2e-exchange-" + Guid.NewGuid().ToString("N")[..8];

        await using var factory = NewFactory(mongoDatabaseName, rabbitMqExchangeName, FakeAuthorizationPort.AllowAll());
        using var client = factory.CreateClient();

        var token = await GetAccessTokenAsync("dev.administrador", "dev.administrador");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogs",
            new { catalogType = CatalogTypes.LossReason, code = "E2E_CREADO", label = "Creado por el test e2e", order = (int?)null, pipelineKind = (string?)null, semanticState = (string?)null },
            RealEstateCrmJsonDefaults.Options);
        createResponse.EnsureSuccessStatusCode();

        var readResponse = await client.GetAsync($"/api/v1/catalogs/{CatalogTypes.LossReason}");
        readResponse.EnsureSuccessStatusCode();

        var result = await readResponse.Content.ReadFromJsonAsync<CatalogQueryResultV1>(RealEstateCrmJsonDefaults.Options);
        Assert.NotNull(result);
        Assert.Contains(result!.Entries, entry => entry.Code == "E2E_CREADO");

        await DropMongoDatabaseAsync(mongoDatabaseName);
    }
}
