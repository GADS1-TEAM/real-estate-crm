using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.BuildingBlocks.Catalogs;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Mongo;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Catalogs;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Events.Access;
using RealEstateCrm.Contracts.Paging;
using RealEstateCrm.Contracts.Parties;
using RealEstateCrm.Contracts.Serialization;
using RealEstateCrm.TestSupport.Authorization;
using RealEstateCrm.TestSupport.Catalogs;

namespace PartyService.Api.Tests;

/// <summary>
/// V2-PTY-001 contra infraestructura real: Mongo (persistencia), RabbitMQ (outbox relay + un
/// consumidor de verdad) y JWT reales emitidos por el Keycloak de <c>docker-compose.yml</c> para
/// <c>dev.administrador</c>/<c>dev.vendedor</c>/<c>dev.responsable</c>. Las llamadas a OTROS
/// servicios (access-service y platform-config-service, que <c>scripts/test-integration.sh</c> no
/// levanta, D10) se sustituyen con los doubles de TestSupport, igual que hizo V2-CAT-001: la matriz
/// rol→permiso replica la de <c>IMPLEMENTATION_REPORT-V2-ACL-001.md</c>. Las reglas propias de
/// party-service (propiedad, estados, relaciones, eventos) corren de verdad.
/// </summary>
/// <remarks>
/// Ninguna espera asume orden entre "el consumidor recibió el evento" y "el outbox lo marcó
/// publicado": ambas condiciones se esperan con polling y timeout.
/// </remarks>
[Trait("Category", "RequiresMongo")]
[Trait("Category", "RequiresRabbitMq")]
[Trait("Category", "RequiresKeycloak")]
public class PartyEndpointsEndToEndTests : IClassFixture<PartyApiFixture>
{
    private readonly PartyApiFixture _api;

    public PartyEndpointsEndToEndTests(PartyApiFixture api)
    {
        _api = api;
    }

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..Math.Min(40, prefix.Length + 9)];

    private async Task<PartyDetailV1> CreateAsync(string user, string route, string name, object? extra = null, Guid? correlationId = null)
    {
        var body = extra ?? new { displayName = name };
        var response = await _api.SendAsync(HttpMethod.Post, route, user, body, correlationId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PartyDetailV1>(RealEstateCrmJsonDefaults.Options))!;
    }

    private async Task<PartyDetailV1> GetAsync(string user, string route, Guid id)
    {
        var response = await _api.SendAsync(HttpMethod.Get, $"{route}/{id}", user);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PartyDetailV1>(RealEstateCrmJsonDefaults.Options))!;
    }

    [Fact]
    public async Task Company_then_contact_then_relationship_then_detail_of_both()
    {
        var company = await CreateAsync(PartyApiFixture.Vendedor, "/api/v1/companies", Unique("Empresa"));
        var contact = await CreateAsync(PartyApiFixture.Vendedor, "/api/v1/contacts", Unique("Contacto"));

        Assert.Equal(PartyKinds.LegalEntity, company.Kind);
        Assert.Equal(PartyKinds.NaturalPerson, contact.Kind);
        Assert.Equal(IdentityStatuses.Active, company.IdentityStatus);
        Assert.Equal(CommercialStatuses.Potential, company.CommercialStatus);
        Assert.Equal(PartyApiFixture.VendedorUserId, company.ResponsibleUserId);

        var relate = await _api.SendAsync(HttpMethod.Post, $"/api/v1/contacts/{contact.PartyId}/relationships", PartyApiFixture.Vendedor, new { companyId = company.PartyId });
        Assert.Equal(HttpStatusCode.Created, relate.StatusCode);

        var companyDetail = await GetAsync(PartyApiFixture.Vendedor, "/api/v1/companies", company.PartyId);
        var contactDetail = await GetAsync(PartyApiFixture.Vendedor, "/api/v1/contacts", contact.PartyId);

        Assert.Equal(contact.PartyId, Assert.Single(companyDetail.Relationships).RelatedPartyId);
        Assert.Equal(company.PartyId, Assert.Single(contactDetail.Relationships).RelatedPartyId);
        Assert.Equal(RelationshipTypes.ContactOf, contactDetail.Relationships[0].RelationshipType);
    }

    [Fact]
    public async Task An_individual_contact_needs_no_company()
    {
        var contact = await CreateAsync(PartyApiFixture.Vendedor, "/api/v1/contacts", Unique("Individual"));

        var detail = await GetAsync(PartyApiFixture.Vendedor, "/api/v1/contacts", contact.PartyId);

        Assert.Empty(detail.Relationships);
        Assert.Null(detail.Email);
        Assert.Null(detail.Phone);
    }

    [Fact]
    public async Task DO_NOT_CONTACT_keeps_the_party_and_is_independent_from_the_identity_status()
    {
        var potential = await CreateAsync(PartyApiFixture.Responsable, "/api/v1/contacts", Unique("Potencial"));
        var blocked = await CreateAsync(PartyApiFixture.Responsable, "/api/v1/contacts", Unique("Bloqueado"));

        var change = await _api.SendAsync(HttpMethod.Post, $"/api/v1/parties/{blocked.PartyId}/commercial-status", PartyApiFixture.Responsable, new { commercialStatus = "DO_NOT_CONTACT" });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        var potentialAfter = await GetAsync(PartyApiFixture.Responsable, "/api/v1/contacts", potential.PartyId);
        var blockedAfter = await GetAsync(PartyApiFixture.Responsable, "/api/v1/contacts", blocked.PartyId);

        Assert.Equal((IdentityStatuses.Active, CommercialStatuses.Potential), (potentialAfter.IdentityStatus, potentialAfter.CommercialStatus));
        Assert.Equal((IdentityStatuses.Active, CommercialStatuses.DoNotContact), (blockedAfter.IdentityStatus, blockedAfter.CommercialStatus));

        var search = await _api.SendAsync(HttpMethod.Get, $"/api/v1/parties?commercialStatus=DO_NOT_CONTACT&q={Uri.EscapeDataString(blocked.DisplayName)}", PartyApiFixture.Responsable);
        var page = (await search.Content.ReadFromJsonAsync<PageV1<PartySummaryV1>>(RealEstateCrmJsonDefaults.Options))!;
        Assert.Equal(blocked.PartyId, Assert.Single(page.Items).PartyId);
    }

    [Fact]
    public async Task Logical_deactivation_request_records_actor_and_date_and_the_party_is_not_removed()
    {
        var company = await CreateAsync(PartyApiFixture.Administrador, "/api/v1/companies", Unique("Baja"));
        var before = DateTimeOffset.UtcNow.AddSeconds(-5);

        var response = await _api.SendAsync(HttpMethod.Post, $"/api/v1/parties/{company.PartyId}/commercial-status", PartyApiFixture.Administrador, new { commercialStatus = "INACTIVE" });
        var inactive = (await response.Content.ReadFromJsonAsync<PartyDetailV1>(RealEstateCrmJsonDefaults.Options))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CommercialStatuses.Inactive, inactive.CommercialStatus);
        Assert.Equal(PartyApiFixture.AdministradorSubject, inactive.CommercialStatusChangedBy);
        Assert.True(inactive.CommercialStatusChangedAt > before);

        var stillThere = await GetAsync(PartyApiFixture.Administrador, "/api/v1/companies", company.PartyId);
        Assert.Equal(company.PartyId, stillThere.PartyId);

        var delete = await _api.SendAsync(HttpMethod.Delete, $"/api/v1/companies/{company.PartyId}", PartyApiFixture.Administrador);
        Assert.True(delete.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound); // no hay borrado físico.
    }

    [Fact]
    public async Task A_vendedor_edits_their_own_party_but_gets_403_on_someone_elses()
    {
        var owned = await CreateAsync(PartyApiFixture.Vendedor, "/api/v1/contacts", Unique("Propio"));
        var foreign = await CreateAsync(PartyApiFixture.Administrador, "/api/v1/contacts", Unique("Ajeno"));

        var okResponse = await _api.SendAsync(HttpMethod.Put, $"/api/v1/contacts/{owned.PartyId}", PartyApiFixture.Vendedor, new { displayName = owned.DisplayName, phone = "+54 11 1234 5678" });
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);

        var forbidden = await _api.SendAsync(HttpMethod.Put, $"/api/v1/contacts/{foreign.PartyId}", PartyApiFixture.Vendedor, new { displayName = "Intento" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var problem = (await forbidden.Content.ReadFromJsonAsync<ProblemDetailsV1>(RealEstateCrmJsonDefaults.Options))!;
        Assert.Equal(ErrorCodes.Forbidden, problem.ErrorCode);
        Assert.Equal("party_not_responsible", problem.Detail);
        Assert.Equal(foreign.DisplayName, (await GetAsync(PartyApiFixture.Administrador, "/api/v1/contacts", foreign.PartyId)).DisplayName);

        var adminEdit = await _api.SendAsync(HttpMethod.Put, $"/api/v1/contacts/{owned.PartyId}", PartyApiFixture.Administrador, new { displayName = owned.DisplayName, phone = "+54 11 0000 0000" });
        Assert.Equal(HttpStatusCode.OK, adminEdit.StatusCode);
    }

    [Fact]
    public async Task Responsable_comercial_changes_status_and_responsible_without_owning_the_party_but_still_needs_ownership_to_edit_data()
    {
        var foreign = await CreateAsync(PartyApiFixture.Vendedor, "/api/v1/contacts", Unique("DeOtro"));

        var status = await _api.SendAsync(HttpMethod.Post, $"/api/v1/parties/{foreign.PartyId}/commercial-status", PartyApiFixture.Responsable, new { commercialStatus = "CUSTOMER" });
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);

        // La propiedad aplica solo a parties.write: editar datos de lo que no es suyo sigue siendo 403.
        var edit = await _api.SendAsync(HttpMethod.Put, $"/api/v1/contacts/{foreign.PartyId}", PartyApiFixture.Responsable, new { displayName = "Intento" });
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);

        // Reasignar tampoco exige ser el responsable actual.
        var responsible = await _api.SendAsync(HttpMethod.Post, $"/api/v1/parties/{foreign.PartyId}/responsible", PartyApiFixture.Responsable, new { responsibleUserId = PartyApiFixture.ResponsableUserId });
        Assert.Equal(HttpStatusCode.OK, responsible.StatusCode);
    }

    [Fact]
    public async Task A_vendedor_cannot_change_the_commercial_status_or_the_responsible()
    {
        var owned = await CreateAsync(PartyApiFixture.Vendedor, "/api/v1/contacts", Unique("SinPermiso"));

        var status = await _api.SendAsync(HttpMethod.Post, $"/api/v1/parties/{owned.PartyId}/commercial-status", PartyApiFixture.Vendedor, new { commercialStatus = "CUSTOMER" });
        var responsible = await _api.SendAsync(HttpMethod.Post, $"/api/v1/parties/{owned.PartyId}/responsible", PartyApiFixture.Vendedor, new { responsibleUserId = PartyApiFixture.ResponsableUserId });

        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, responsible.StatusCode);
    }

    [Fact]
    public async Task Origin_is_validated_against_the_catalog_and_stored_with_its_version()
    {
        var valid = await CreateAsync(PartyApiFixture.Vendedor, "/api/v1/contacts", "x", new { displayName = Unique("ConOrigen"), originCode = "REFERIDO" });
        Assert.Equal("REFERIDO", valid.OriginCode);
        Assert.Equal(PartyApiFixture.CatalogVersion, valid.OriginCatalogVersion);

        var invalid = await _api.SendAsync(HttpMethod.Post, "/api/v1/contacts", PartyApiFixture.Vendedor, new { displayName = Unique("OrigenMalo"), originCode = "INVENTADO" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Equal("party_invalid_origin", (await invalid.Content.ReadFromJsonAsync<ProblemDetailsV1>(RealEstateCrmJsonDefaults.Options))!.ErrorCode);
    }

    [Fact]
    public async Task Duplicates_are_allowed_there_is_no_unique_index_on_tax_id_email_or_phone()
    {
        var payload = new { displayName = "Duplicada SA", taxIdentifier = "30-99999999-9", email = "dup@norte.com", phone = "+54 11 9999 9999" };

        var first = await _api.SendAsync(HttpMethod.Post, "/api/v1/companies", PartyApiFixture.Vendedor, payload);
        var second = await _api.SendAsync(HttpMethod.Post, "/api/v1/companies", PartyApiFixture.Administrador, payload);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var indexes = await _api.ListPartyIndexesAsync();
        Assert.DoesNotContain(indexes, index => index.GetValue("unique", false).ToBoolean() && index["name"].AsString != "_id_");
    }

    [Fact]
    public async Task Validation_errors_are_problem_details_and_unauthenticated_requests_are_401()
    {
        var noName = await _api.SendAsync(HttpMethod.Post, "/api/v1/contacts", PartyApiFixture.Vendedor, new { displayName = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, noName.StatusCode);
        Assert.Equal(ErrorCodes.ValidationError, (await noName.Content.ReadFromJsonAsync<ProblemDetailsV1>(RealEstateCrmJsonDefaults.Options))!.ErrorCode);

        var badEmail = await _api.SendAsync(HttpMethod.Post, "/api/v1/contacts", PartyApiFixture.Vendedor, new { displayName = "Ana", email = "no-es-un-email" });
        Assert.Equal(HttpStatusCode.BadRequest, badEmail.StatusCode);

        var onlyName = await _api.SendAsync(HttpMethod.Post, "/api/v1/contacts", PartyApiFixture.Vendedor, new { displayName = Unique("SoloNombre") });
        Assert.Equal(HttpStatusCode.Created, onlyName.StatusCode); // nombre es lo único obligatorio.

        var anonymous = await _api.SendAsync(HttpMethod.Get, "/api/v1/parties", user: null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Reassigning_the_responsible_is_published_end_to_end_with_the_original_correlation_id()
    {
        var contact = await CreateAsync(PartyApiFixture.Vendedor, "/api/v1/contacts", Unique("Reasignar"));
        var correlationId = Guid.NewGuid();

        var captured = await _api.ListenForAsync<ResponsibleAssignedV1>("PartyResponsibleAssigned", async () =>
        {
            var response = await _api.SendAsync(
                HttpMethod.Post,
                $"/api/v1/parties/{contact.PartyId}/responsible",
                PartyApiFixture.Responsable,
                new { responsibleUserId = PartyApiFixture.AdministradorUserId },
                correlationId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = (await response.Content.ReadFromJsonAsync<PartyDetailV1>(RealEstateCrmJsonDefaults.Options))!;
            Assert.Equal(PartyApiFixture.AdministradorUserId, updated.ResponsibleUserId);
        }, match: envelope => envelope.Payload.ResourceId == contact.PartyId);

        Assert.Equal(ResourceTypes.Party, captured.Payload.ResourceType);
        Assert.Equal(contact.PartyId, captured.Payload.ResourceId);
        Assert.Equal(PartyApiFixture.VendedorUserId, captured.Payload.PreviousResponsibleUserId);
        Assert.Equal(PartyApiFixture.AdministradorUserId, captured.Payload.NewResponsibleUserId);
        Assert.Equal(PartyApiFixture.ResponsableUserId, captured.Payload.AssignedByUserId);
        Assert.Equal(PartyApiFixture.ResponsableSubject, captured.ActorId);
        Assert.Equal(correlationId, captured.CorrelationId);

        // El outbox lo marca publicado (esperado por polling, sin asumir orden con el consumidor).
        await _api.WaitForOutboxPublishedAsync("PartyResponsibleAssigned", contact.PartyId);

        // La reasignación cambió quién puede editar: el Vendedor anterior ya no, el nuevo responsable sí.
        var previous = await _api.SendAsync(HttpMethod.Put, $"/api/v1/contacts/{contact.PartyId}", PartyApiFixture.Vendedor, new { displayName = "Ya no soy responsable" });
        Assert.Equal(HttpStatusCode.Forbidden, previous.StatusCode);
    }

    [Fact]
    public async Task Registering_a_party_publishes_PartyRegistered_with_actor_and_correlation()
    {
        var correlationId = Guid.NewGuid();
        PartyDetailV1? created = null;

        var captured = await _api.ListenForAsync<PartyRegisteredPayload>("PartyRegistered", async () =>
        {
            created = await CreateAsync(PartyApiFixture.Vendedor, "/api/v1/companies", Unique("Publicada"), correlationId: correlationId);
        }, match: envelope => envelope.Payload.DisplayName.StartsWith("Publicada", StringComparison.Ordinal));

        Assert.Equal(created!.PartyId, captured.Payload.PartyId);
        Assert.Equal("ACTIVE", captured.Payload.IdentityStatus);
        Assert.Equal("POTENTIAL", captured.Payload.CommercialStatus);
        Assert.Equal(PartyApiFixture.VendedorSubject, captured.ActorId);
        Assert.Equal(correlationId, captured.CorrelationId);

        await _api.WaitForOutboxPublishedAsync("PartyRegistered", created.PartyId);
    }

    public sealed record PartyRegisteredPayload(Guid PartyId, string Kind, string DisplayName, string IdentityStatus, string CommercialStatus);
}

/// <summary>Factory compartida (una base Mongo y un exchange RabbitMQ efímeros por clase de tests) más helpers de HTTP, RabbitMQ y polling.</summary>
public sealed class PartyApiFixture : IAsyncLifetime
{
    public const string Administrador = "dev.administrador";
    public const string Vendedor = "dev.vendedor";
    public const string Responsable = "dev.responsable";

    // sub de Keycloak (id fijo del realm-export) y userId de access-service que asigna el fake.
    public static readonly Guid AdministradorSubject = Guid.Parse("a0000000-0000-4000-8000-000000000001");
    public static readonly Guid VendedorSubject = Guid.Parse("a0000000-0000-4000-8000-000000000002");
    public static readonly Guid ResponsableSubject = Guid.Parse("a0000000-0000-4000-8000-000000000003");
    public static readonly Guid AdministradorUserId = Guid.Parse("b0000000-0000-4000-8000-000000000001");
    public static readonly Guid VendedorUserId = Guid.Parse("b0000000-0000-4000-8000-000000000002");
    public static readonly Guid ResponsableUserId = Guid.Parse("b0000000-0000-4000-8000-000000000003");
    public const int CatalogVersion = 4;

    private static readonly string KeycloakBaseUrl = Environment.GetEnvironmentVariable("KEYCLOAK_BASE_URL") ?? "http://localhost:8080";
    private static readonly string KeycloakRealm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? "crm-dev";
    private static readonly string KeycloakClientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "operations-bff";
    private static readonly string MongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017/?directConnection=true";

    private static readonly IReadOnlyDictionary<string, string[]> PermissionsByRole = new Dictionary<string, string[]>
    {
        ["Administrador"] = new[] { Permissions.UsersRead, Permissions.UsersManage, Permissions.CatalogsRead, Permissions.CatalogsManage, Permissions.PartiesRead, Permissions.PartiesWrite, Permissions.PartiesChangeCommercialStatus, Permissions.PartiesAssignResponsible },
        ["Vendedor"] = new[] { Permissions.UsersRead, Permissions.CatalogsRead, Permissions.PartiesRead, Permissions.PartiesWrite },
        ["Responsable Comercial"] = new[] { Permissions.UsersRead, Permissions.CatalogsRead, Permissions.PartiesRead, Permissions.PartiesWrite, Permissions.PartiesChangeCommercialStatus, Permissions.PartiesAssignResponsible },
    };

    private static readonly IReadOnlyDictionary<Guid, (Guid UserId, string Role)> UsersBySubject = new Dictionary<Guid, (Guid, string)>
    {
        [AdministradorSubject] = (AdministradorUserId, "Administrador"),
        [VendedorSubject] = (VendedorUserId, "Vendedor"),
        [ResponsableSubject] = (ResponsableUserId, "Responsable Comercial"),
    };

    private readonly string _mongoDatabaseName = "pty001_e2e_" + Guid.NewGuid().ToString("N");
    private readonly string _exchangeName = "pty001-e2e-exchange-" + Guid.NewGuid().ToString("N")[..8];
    private readonly Dictionary<string, string> _tokens = new();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
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
                    ["RabbitMq:ServiceExchangeName"] = _exchangeName,
                    ["OutboxRelay:PollingInterval"] = "00:00:00.200",
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthorizationPort>();
                services.AddSingleton<IAuthorizationPort>(new FakeAuthorizationPort((actor, permission, _, _) =>
                    UsersBySubject.TryGetValue(actor, out var user) && PermissionsByRole[user.Role].Contains(permission, StringComparer.Ordinal)
                        ? AuthorizationDecision.Allow()
                        : AuthorizationDecision.Deny(DenyReasons.PermissionNotGranted)));

                var directory = new FakeUserDirectoryPort();
                foreach (var (subject, user) in UsersBySubject)
                {
                    directory.WithActiveUser(subject, user.UserId, user.Role);
                }

                services.RemoveAll<IUserDirectoryPort>();
                services.AddSingleton<IUserDirectoryPort>(directory);

                services.RemoveAll<IResponsibleAssignmentValidationPort>();
                services.AddSingleton<IResponsibleAssignmentValidationPort>(new FakeResponsibleAssignmentValidationPort((actor, _, _, responsible) =>
                    !UsersBySubject.TryGetValue(actor, out var user) || !PermissionsByRole[user.Role].Contains(Permissions.PartiesAssignResponsible, StringComparer.Ordinal)
                        ? AuthorizationDecision.Deny(DenyReasons.PermissionNotGranted)
                        : UsersBySubject.Values.Any(u => u.UserId == responsible)
                            ? AuthorizationDecision.Allow()
                            : AuthorizationDecision.Deny(ResponsibleAssignmentDenyReasons.ResponsibleUserNotFound)));

                services.RemoveAll<ICatalogReaderPort>();
                services.AddSingleton<ICatalogReaderPort>(new FakeCatalogReaderPort { CatalogVersion = CatalogVersion }
                    .WithEntry(CatalogTypes.CommercialOrigin, "REFERIDO")
                    .WithEntry(CatalogTypes.CommercialOrigin, "WHATSAPP"));
            });
        });

        _client = _factory.CreateClient();

        foreach (var user in new[] { Administrador, Vendedor, Responsable })
        {
            _tokens[user] = await GetAccessTokenAsync(user);
        }
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();

        using var mongoClient = new MongoClient(MongoConnectionString);
        await mongoClient.DropDatabaseAsync(_mongoDatabaseName);
    }

    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? user, object? body = null, Guid? correlationId = null)
    {
        var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: RealEstateCrmJsonDefaults.Options);
        }

        if (user is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[user]);
        }

        if (correlationId is not null)
        {
            request.Headers.Add("X-Correlation-Id", correlationId.ToString());
        }

        return _client.SendAsync(request);
    }

    public async Task<IReadOnlyList<MongoDB.Bson.BsonDocument>> ListPartyIndexesAsync()
    {
        using var mongoClient = new MongoClient(MongoConnectionString);
        var collection = mongoClient.GetDatabase(_mongoDatabaseName).GetCollection<MongoDB.Bson.BsonDocument>("parties");
        return await (await collection.Indexes.ListAsync()).ToListAsync();
    }

    /// <summary>Espera, con polling y timeout, a que el outbox marque publicado el evento <paramref name="eventName"/> del aggregate.</summary>
    public async Task WaitForOutboxPublishedAsync(string eventName, Guid aggregateId)
    {
        using var mongoClient = new MongoClient(MongoConnectionString);
        var outbox = mongoClient.GetDatabase(_mongoDatabaseName).GetCollection<OutboxMessage>("outbox_messages");

        await WaitUntilAsync(
            async () => await outbox.CountDocumentsAsync(m => m.Name == eventName && m.AggregateId == aggregateId && m.PublishedAt != null) > 0,
            TimeSpan.FromSeconds(15),
            $"el outbox nunca marcó publicado '{eventName}' del aggregate {aggregateId}");
    }

    public static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, string failureMessage)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail($"Timeout de {timeout.TotalSeconds:0}s: {failureMessage}.");
    }

    /// <summary>
    /// Se suscribe a <paramref name="eventName"/> en el exchange real del servicio, dispara
    /// <paramref name="triggerAsync"/> y espera con timeout el primer evento que cumpla
    /// <paramref name="match"/>.
    /// </summary>
    public async Task<EventEnvelopeV1<TPayload>> ListenForAsync<TPayload>(
        string eventName,
        Func<Task> triggerAsync,
        Func<EventEnvelopeV1<TPayload>, bool>? match = null)
    {
        var connectionProvider = new RabbitMqConnectionProvider(Options.Create(new RabbitMqOptions
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
        }));

        await using (connectionProvider)
        {
            using var mongoClient = new MongoClient(MongoConnectionString);
            var inbox = new MongoInbox(mongoClient.GetDatabase("pty001_e2e_inbox_" + Guid.NewGuid().ToString("N")));

            var consumer = new CapturingConsumer<TPayload>(match ?? (_ => true));
            var host = new RabbitMqEventConsumerHost(connectionProvider);
            var registration = new RabbitMqConsumerRegistration(_exchangeName, eventName, "pty001-e2e-queue-" + Guid.NewGuid().ToString("N")[..8]);
            await using var channel = await host.StartAsync(registration, consumer, inbox);

            await triggerAsync();

            return await consumer.WaitAsync(TimeSpan.FromSeconds(20));
        }
    }

    private sealed class CapturingConsumer<TPayload>(Func<EventEnvelopeV1<TPayload>, bool> match) : IEventConsumer<TPayload>
    {
        private readonly TaskCompletionSource<EventEnvelopeV1<TPayload>> _received = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string ConsumerName => "pty001-e2e-consumer";

        public Task HandleAsync(EventEnvelopeV1<TPayload> envelope, CancellationToken cancellationToken = default)
        {
            if (match(envelope))
            {
                _received.TrySetResult(envelope);
            }

            return Task.CompletedTask;
        }

        public Task<EventEnvelopeV1<TPayload>> WaitAsync(TimeSpan timeout) => _received.Task.WaitAsync(timeout);
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
}
