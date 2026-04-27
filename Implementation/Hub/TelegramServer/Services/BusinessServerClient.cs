using System.Net.Http.Json;
using Common.Dto;

namespace TelegramServer.Services;

public class BusinessServerClient : IBusinessServerClient
{
    private readonly HttpClient _http;
    private readonly ILogger<BusinessServerClient> _logger;

    public BusinessServerClient(HttpClient http, ILogger<BusinessServerClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<EchoResponse?> EchoAsync(EchoRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/echo", request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EchoResponse>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call BusinessServer echo endpoint");
            return null;
        }
    }
}
