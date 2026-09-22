using ActivityService.Application.Commands;
using ActivityService.Application.Queries;
using ActivityService.Domain;
using ActivityService.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RealEstateCrm.BuildingBlocks.Infrastructure.Persistence.Mongo;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MongoDB
builder.Services.AddMongoPersistence(builder.Configuration);

// Register Repositories and Handlers
builder.Services.AddScoped<IActivityRepository, MongoActivityRepository>();
builder.Services.AddScoped<RecordActivityHandler>();
builder.Services.AddScoped<TimelineQueryHandler>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => "Activity Service API is running");

app.Run();
