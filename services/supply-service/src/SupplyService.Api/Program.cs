using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using SupplyService.Application.Commands;
using SupplyService.Application.Ports;
using SupplyService.Infrastructure.Mongo;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMongoPersistence(builder.Configuration);

builder.Services.AddScoped<IListingRepository, VersionedMongoRepository>();
builder.Services.AddScoped<IOutboxPort, DevNullOutboxPort>();
builder.Services.AddScoped<IPropertyReferencePort, FakePropertyReferencePort>();
builder.Services.AddScoped<ICatalogReaderPort, FakeCatalogReaderPort>();

builder.Services.AddScoped<CreateListingHandler>();
builder.Services.AddScoped<UpdateListingHandler>();
builder.Services.AddScoped<ActivateListingHandler>();
builder.Services.AddScoped<PauseListingHandler>();
builder.Services.AddScoped<CloseListingHandler>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Supply API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();
public partial class Program { }

public class DevNullOutboxPort : IOutboxPort
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : class => Task.CompletedTask;
}

public class FakePropertyReferencePort : IPropertyReferencePort
{
    public Task<PropertyReferenceResult> GetPropertyAsync(string propertyId, CancellationToken cancellationToken) =>
        Task.FromResult(new PropertyReferenceResult(true, true));
}

public class FakeCatalogReaderPort : ICatalogReaderPort
{
    public Task<bool> IsValidOperationTypeCodeAsync(string operationTypeCode, CancellationToken cancellationToken) =>
        Task.FromResult(true);
}
