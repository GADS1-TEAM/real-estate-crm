using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DemandService.Application.Commands;
using DemandService.Domain.Aggregates;
using DemandService.Infrastructure.Persistence.Mongo;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMongoPersistence(builder.Configuration);

builder.Services.AddScoped<RequirementRepository>();
builder.Services.AddScoped<RealEstateCrm.BuildingBlocks.Persistence.IRepository<Requirement, string>>(sp => sp.GetRequiredService<RequirementRepository>());
builder.Services.AddScoped<CreateRequirementHandler>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Demand API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();
public partial class Program { }
