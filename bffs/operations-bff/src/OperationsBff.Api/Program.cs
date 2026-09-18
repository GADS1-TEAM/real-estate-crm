using OperationsBff.Api.AccessService;
using OperationsBff.Api.ErrorHandling;
using OperationsBff.Api.PartyService;
using OperationsBff.Api.PlatformConfigService;
using RealEstateCrm.BuildingBlocks.Infrastructure.Authentication;
using RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;
using RealEstateCrm.BuildingBlocks.Infrastructure.Observability;
using RealEstateCrm.Contracts.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = RealEstateCrmJsonDefaults.Options.PropertyNamingPolicy;
        options.JsonSerializerOptions.DefaultIgnoreCondition = RealEstateCrmJsonDefaults.Options.DefaultIgnoreCondition;

        foreach (var converter in RealEstateCrmJsonDefaults.Options.Converters)
        {
            options.JsonSerializerOptions.Converters.Add(converter);
        }
    });

builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

// Primer wire-up real de auth en el BFF (V2-ACL-001): cookie de sesión OIDC (AUTH-001,
// V2-FND-002). El front nunca ve el access token.
builder.Services.AddKeycloakOpenIdConnectCookieAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// Token relay (D6): el BFF reenvía el access token de la sesión como Bearer a access-service.
var accessServiceBaseUrl = builder.Configuration["AccessService:BaseUrl"]
    ?? throw new InvalidOperationException("Falta configuración 'AccessService:BaseUrl'.");

builder.Services.AddHttpClient<AccessServiceClient>(client => client.BaseAddress = new Uri(accessServiceBaseUrl));

// V2-CAT-001: mismo token relay (D6) hacia platform-config-service. Registro agregado al
// wire-up base de V2-ACL-001 sin reestructurarlo (plan Wave 2 §7.3).
var platformConfigServiceBaseUrl = builder.Configuration["PlatformConfigService:BaseUrl"]
    ?? throw new InvalidOperationException("Falta configuración 'PlatformConfigService:BaseUrl'.");

builder.Services.AddHttpClient<PlatformConfigServiceClient>(client => client.BaseAddress = new Uri(platformConfigServiceBaseUrl));

// V2-PTY-001: mismo token relay (D6) hacia party-service.
var partyServiceBaseUrl = builder.Configuration["PartyService:BaseUrl"]
    ?? throw new InvalidOperationException("Falta configuración 'PartyService:BaseUrl'.");

builder.Services.AddHttpClient<PartyServiceClient>(client => client.BaseAddress = new Uri(partyServiceBaseUrl));

builder.Services.AddCrmHealthChecks();
builder.Services.AddCrmObservability("operations-bff");

var app = builder.Build();

app.UseExceptionHandler();
app.UseCrmCorrelationId();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapCrmHealthEndpoints();

app.Run();

/// <summary>Punto de extensión para <c>WebApplicationFactory&lt;Program&gt;</c> en tests de integración.</summary>
public partial class Program;
