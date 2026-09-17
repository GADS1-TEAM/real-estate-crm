using AccessService.Api.ErrorHandling;
using AccessService.Api.ExecutionContextResolution;
using AccessService.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using RealEstateCrm.BuildingBlocks.Infrastructure.Authentication;
using RealEstateCrm.BuildingBlocks.Infrastructure.HealthChecks;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.Outbox;
using RealEstateCrm.BuildingBlocks.Infrastructure.Messaging.RabbitMq;
using RealEstateCrm.BuildingBlocks.Infrastructure.Observability;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;
using RealEstateCrm.Contracts.Errors;
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

// Validación en el borde con forma de respuesta estable (aspnetcore-rest-layer skill): un
// [Required]/[EmailAddress] roto en el body nunca sale con el ValidationProblemDetails por
// defecto de ASP.NET Core, sale como ProblemDetailsV1.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        var correlationId = context.HttpContext.Response.Headers.TryGetValue("X-Correlation-Id", out var headerValue)
            && Guid.TryParse(headerValue.ToString(), out var parsedCorrelationId)
                ? parsedCorrelationId
                : Guid.NewGuid();

        var problem = new ProblemDetailsV1(
            Type: "about:blank",
            Title: "Error de validación.",
            Status: StatusCodes.Status400BadRequest,
            Detail: null,
            Instance: context.HttpContext.Request.Path,
            ErrorCode: ErrorCodes.ValidationError,
            CorrelationId: correlationId,
            Errors: errors);

        return new BadRequestObjectResult(problem);
    };
});

builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentExecutionContextProvider>();

builder.Services.AddMongoPersistence(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddOutboxRelay(builder.Configuration);

builder.Services.AddKeycloakJwtBearerAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddAccessServiceInfrastructure();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAccessServiceDevSeed();
}

builder.Services.AddCrmHealthChecks()
    .AddMongoReadinessCheck()
    .AddRabbitMqReadinessCheck();

builder.Services.AddCrmObservability("access-service");

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
