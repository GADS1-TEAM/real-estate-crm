var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseRouting();
app.MapControllers();

app.MapGet("/", () => new
{
    service = "PlatformAdminBff",
    status = "Running",
    health = "/health",
    api = "/api/v1/admin/status"
});

app.Run();
