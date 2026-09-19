using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Serialization;

namespace AccessService.Api.Tests;

/// <summary>
/// Cierra el follow-up de V2-FND-003: "un request de prueba conserva correlationId desde BFF
/// hasta consumer" no se pudo demostrar end-to-end ahí porque no había ningún endpoint HTTP
/// real. Acá sí: entra por <c>X-Correlation-Id</c> (el mismo header que <c>CorrelationIdMiddleware</c>
/// toma como autoritativo si ya viene seteado, exactamente lo que hace <c>operations-bff</c> al
/// reenviar la request) y se verifica hasta el evento publicado en RabbitMQ real.
/// </summary>
/// <remarks>
/// Excluido por defecto: <c>dotnet test --filter "Category!=RequiresMongo&amp;Category!=RequiresRabbitMq&amp;Category!=RequiresKeycloak"</c>.
/// Necesita Mongo, RabbitMQ y el Keycloak real de <c>docker-compose.yml</c> con el realm-export
/// de esta task (usuarios <c>dev.administrador</c>/<c>dev.vendedor</c> con <c>id</c> fijo).
/// </remarks>
[Trait("Category", "RequiresMongo")]
[Trait("Category", "RequiresRabbitMq")]
[Trait("Category", "RequiresKeycloak")]
public class CorrelationIdAndAuthorizationEndToEndTests : IAsyncLifetime
{
    private static readonly string KeycloakBaseUrl = Environment.GetEnvironmentVariable("KEYCLOAK_BASE_URL") ?? "http://localhost:8080";
    private static readonly string KeycloakRealm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? "crm-dev";
    private static readonly string KeycloakClientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "operations-bff";

    private readonly string _mongoDatabaseName = "acl001_e2e_" + Guid.NewGuid().ToString("N");
    private readonly string _rabbitMqExchangeName = "acl001-e2e-exchange-" + Guid.NewGuid().ToString("N")[..8];

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

        var mongoDatabaseName = _mongoDatabaseName;
        using var mongoClient = new MongoDB.Driver.MongoClient(
            Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017/?directConnection=true");
        await mongoClient.DropDatabaseAsync(mongoDatabaseName);
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
    public async Task Vendedor_creating_a_user_is_rejected_with_a_403_problem_details()
    {
        var vendedorToken = await GetAccessTokenAsync("dev.vendedor", "dev.vendedor");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vendedorToken);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/users",
            new { keycloakSubject = Guid.NewGuid(), displayName = "No Deberia Crearse", email = "x@crm-dev.local" },
            RealEstateCrmJsonDefaults.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsV1>(RealEstateCrmJsonDefaults.Options);
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.Forbidden, problem!.ErrorCode);
    }

    [Fact]
    public async Task Correlation_id_survives_from_the_incoming_header_to_the_published_event()
    {
        var adminToken = await GetAccessTokenAsync("dev.administrador", "dev.administrador");
        var correlationId = Guid.NewGuid();

        var consumedEnvelope = await ListenForNextUserCreatedEventAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
            {
                Content = JsonContent.Create(
                    new { keycloakSubject = Guid.NewGuid(), displayName = "Correlacionado", email = "correlacionado@crm-dev.local" },
                    options: RealEstateCrmJsonDefaults.Options),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Headers.Add("X-Correlation-Id", correlationId.ToString());

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            Assert.Equal(correlationId.ToString(), response.Headers.GetValues("X-Correlation-Id").Single());
        });

        Assert.Equal(correlationId, consumedEnvelope.CorrelationId);
    }

    private sealed record UserCreatedPayload(Guid UserId, string DisplayName, string Email, string Status);

    private sealed class CapturingConsumer : IEventConsumer<UserCreatedPayload>
    {
        public string ConsumerName => "e2e-test-consumer";
        private readonly TaskCompletionSource<EventEnvelopeV1<UserCreatedPayload>> _received = new();

        public Task HandleAsync(EventEnvelopeV1<UserCreatedPayload> envelope, CancellationToken cancellationToken = default)
        {
            _received.TrySetResult(envelope);
            return Task.CompletedTask;
        }

        public Task<EventEnvelopeV1<UserCreatedPayload>> WaitAsync(TimeSpan timeout) => _received.Task.WaitAsync(timeout);
    }

    private async Task<EventEnvelopeV1<UserCreatedPayload>> ListenForNextUserCreatedEventAsync(Func<Task> triggerAsync)
    {
        var connectionProvider = new RabbitMqConnectionProvider(Options.Create(new RabbitMqOptions
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
        }));

        await using (connectionProvider)
        {
            using var mongoClient = new MongoDB.Driver.MongoClient(
                Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017/?directConnection=true");
            var inbox = new MongoInbox(mongoClient.GetDatabase("acl001_e2e_inbox_" + Guid.NewGuid().ToString("N")));

            var host = new RabbitMqEventConsumerHost(connectionProvider);
            var consumer = new CapturingConsumer();
            var queueName = "acl001-e2e-queue-" + Guid.NewGuid().ToString("N")[..8];
            var registration = new RabbitMqConsumerRegistration(_rabbitMqExchangeName, "UserCreated", queueName);
            await using var channel = await host.StartAsync(registration, consumer, inbox);

            await triggerAsync();

            return await consumer.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }
}
