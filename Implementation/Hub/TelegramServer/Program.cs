using Telegram.Bot;
using TelegramServer;
using TelegramServer.Services;

var builder = Host.CreateApplicationBuilder(args);

var token = builder.Configuration["Telegram:BotToken"]
    ?? throw new InvalidOperationException("Telegram:BotToken is not configured.");

builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));
builder.Services.AddHttpClient<IBusinessServerClient, BusinessServerClient>(client =>
{
    var baseUrl = builder.Configuration["BusinessServer:BaseUrl"] ?? "http://business-server:8080";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddSingleton<BotUpdateHandler>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
