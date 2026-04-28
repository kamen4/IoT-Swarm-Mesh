using BusinessServer.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IDeviceRegistryService, DeviceRegistryService>();
builder.Services.AddSingleton<IUserService, UserService>();

// Redis
var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "redis:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

// Pin toggle
builder.Services.AddSingleton<IPinDispatchService, PinDispatchService>();
builder.Services.AddHostedService<PinEventListener>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow }));

app.Run();
