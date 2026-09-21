using ActivityService.Application.Commands;
using ActivityService.Application.Queries;
using ActivityService.Domain;
using ActivityService.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MongoDB
var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoClient = new MongoClient(mongoConnectionString);
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddScoped<IMongoDatabase>(sp => 
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("ActivityDb");
});

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
