using BusinessServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IDeviceRegistryService, DeviceRegistryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow }));

app.Run();
