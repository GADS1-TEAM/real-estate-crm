using OperationsBff.Api.AccessService;
using OperationsBff.Api.ActivityService;
using OperationsBff.Api.AnalyticsService;
using OperationsBff.Api.AutomationAiService;
using OperationsBff.Api.CommercialService;
using OperationsBff.Api.DemandService;
using OperationsBff.Api.ErrorHandling;
using OperationsBff.Api.MatchingService;
using OperationsBff.Api.PartyService;
using OperationsBff.Api.PlatformConfigService;
using OperationsBff.Api.PropertyService;
using OperationsBff.Api.SupplyService;
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

var propertyServiceBaseUrl = builder.Configuration["PropertyService:BaseUrl"]
    ?? throw new InvalidOperationException("Falta configuracion property");
builder.Services.AddHttpClient<PropertyServiceClient>(client => client.BaseAddress = new Uri(propertyServiceBaseUrl));

var supplyServiceBaseUrl = builder.Configuration["SupplyService:BaseUrl"]
    ?? throw new InvalidOperationException("Falta configuracion supply");
builder.Services.AddHttpClient<SupplyServiceClient>(client => client.BaseAddress = new Uri(supplyServiceBaseUrl));

// Servicios de dominio restantes: demand, matching, commercial, activity, analytics, automation-ai.
var demandServiceBaseUrl = builder.Configuration["DemandService:BaseUrl"] ?? "http://localhost:5220";
builder.Services.AddHttpClient<DemandServiceClient>(client => client.BaseAddress = new Uri(demandServiceBaseUrl));

var matchingServiceBaseUrl = builder.Configuration["MatchingService:BaseUrl"] ?? "http://localhost:5290";
builder.Services.AddHttpClient<MatchingServiceClient>(client => client.BaseAddress = new Uri(matchingServiceBaseUrl));

var commercialServiceBaseUrl = builder.Configuration["CommercialService:BaseUrl"] ?? "http://localhost:5145";
builder.Services.AddHttpClient<CommercialServiceClient>(client => client.BaseAddress = new Uri(commercialServiceBaseUrl));

var activityServiceBaseUrl = builder.Configuration["ActivityService:BaseUrl"] ?? "http://localhost:5247";
builder.Services.AddHttpClient<ActivityServiceClient>(client => client.BaseAddress = new Uri(activityServiceBaseUrl));

var analyticsServiceBaseUrl = builder.Configuration["AnalyticsService:BaseUrl"] ?? "http://localhost:5046";
builder.Services.AddHttpClient<AnalyticsServiceClient>(client => client.BaseAddress = new Uri(analyticsServiceBaseUrl));

var automationAiServiceBaseUrl = builder.Configuration["AutomationAiService:BaseUrl"] ?? "http://localhost:5015";
builder.Services.AddHttpClient<AutomationAiServiceClient>(client => client.BaseAddress = new Uri(automationAiServiceBaseUrl));

// CORS: el frontend Next.js corre en localhost:3000 y necesita acceder al BFF.
builder.Services.AddCors(options =>
{
    options.AddPolicy("CrmWeb", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddCrmHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Operations BFF API v1");
    c.RoutePrefix = "swagger";
});

app.UseExceptionHandler();
app.UseCrmCorrelationId();
app.UseCors("CrmWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapCrmHealthEndpoints();

app.Run();

/// <summary>Punto de extensión para <c>WebApplicationFactory&lt;Program&gt;</c> en tests de integración.</summary>
public partial class Program;
