using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyService.Api.ErrorHandling;
using PropertyService.Application;
using PropertyService.Domain.Aggregates;
using PropertyService.Infrastructure.Persistence.Mongo;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;
using PropertyService.Application.Ports;
using RealEstateCrm.BuildingBlocks.Infrastructure.References;
using RealEstateCrm.BuildingBlocks.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddMongoPersistence(builder.Configuration);
builder.Services.AddPartyReferenceHttpClient(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IOutbox, DevNullOutbox>();

builder.Services.AddScoped<PropertyService.Application.Ports.IPartyReferencePort, PropertyServicePartyReferenceAdapter>();
builder.Services.AddScoped<PropertyRepository>();
builder.Services.AddScoped<PropertyInterestRepository>();
builder.Services.AddScoped<RealEstateCrm.BuildingBlocks.Persistence.IRepository<Property, string>>(sp => sp.GetRequiredService<PropertyRepository>());
builder.Services.AddScoped<RealEstateCrm.BuildingBlocks.Persistence.IRepository<PropertyInterest, string>>(sp => sp.GetRequiredService<PropertyInterestRepository>());
builder.Services.AddScoped<PropertyManagementService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Property API v1");
    c.RoutePrefix = "swagger";
});

app.UseExceptionHandler();
app.MapControllers();

app.Run();
public partial class Program { }

public class PropertyServicePartyReferenceAdapter : PropertyService.Application.Ports.IPartyReferencePort
{
    private readonly RealEstateCrm.BuildingBlocks.References.IPartyReferencePort _buildingBlocksPort;

    public PropertyServicePartyReferenceAdapter(RealEstateCrm.BuildingBlocks.References.IPartyReferencePort buildingBlocksPort)
    {
        _buildingBlocksPort = buildingBlocksPort;
    }

    public async Task<bool> ExistsAsync(string partyId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(partyId, out var partyGuid)) return true;
        try
        {
            var party = await _buildingBlocksPort.GetAsync(partyGuid, cancellationToken);
            return party != null;
        }
        catch
        {
            return true;
        }
    }
}

public class DevNullOutbox : IOutbox
{
    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>(Array.Empty<OutboxMessage>());
    public Task MarkPublishedAsync(Guid eventId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
