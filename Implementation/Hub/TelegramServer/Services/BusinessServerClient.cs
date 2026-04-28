using System.Net.Http.Json;
using Common.Dto;
using Common.Entities;
using Common.Enums;

namespace TelegramServer.Services;

/// <summary>
/// HTTP client implementation of <see cref="IBusinessServerClient"/>.
/// Wraps all BusinessServer API calls with try/catch so that a failed
/// HTTP request returns null/empty rather than propagating an exception.
/// </summary>
public sealed class BusinessServerClient : IBusinessServerClient
{
    private readonly HttpClient _http;
    private readonly ILogger<BusinessServerClient> _logger;

    /// <summary>Initialises the client with an <see cref="HttpClient"/> pre-configured with the BusinessServer base URL.</summary>
    public BusinessServerClient(HttpClient http, ILogger<BusinessServerClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<UserDto?> RegisterUserAsync(long telegramId, string username, CancellationToken ct = default)
        => await PostJsonAsync<RegisterUserRequest, UserDto>("/api/users/register", new(telegramId, username), ct);

    public async Task<UserDto?> GetUserAsync(long telegramId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync($"/api/users/{telegramId}", ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<UserDto>(ct);
        }
        catch (Exception ex) { _logger.LogError(ex, "GetUser failed"); return null; }
    }

    public async Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("/api/users", ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<List<UserDto>>(ct) ?? [];
        }
        catch (Exception ex) { _logger.LogError(ex, "GetAllUsers failed"); return []; }
    }

    public async Task InviteUserAsync(long telegramId, string username, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/users/invite", new InviteUserRequest(telegramId, username), ct);
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex) { _logger.LogError(ex, "InviteUser failed"); }
    }

    public async Task InviteUserByUsernameAsync(string username, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/users/invite-by-username", new InviteUserByUsernameRequest(username), ct);
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex) { _logger.LogError(ex, "InviteUserByUsername failed"); }
    }

    public async Task SetRoleAsync(long telegramId, UserRole role, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync($"/api/users/{telegramId}/role", new SetRoleRequest(telegramId, role), ct);
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex) { _logger.LogError(ex, "SetRole failed"); }
    }

    public async Task RemoveUserAsync(long telegramId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.DeleteAsync($"/api/users/{telegramId}", ct);
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex) { _logger.LogError(ex, "RemoveUser failed"); }
    }

    public async Task<PinToggleResponse?> TogglePinAsync(int pin, CancellationToken ct = default)
        => await PostJsonAsync<PinToggleRequest, PinToggleResponse>("/api/pin/toggle", new(pin), ct);

    public async Task<List<DeviceInfo>> GetDevicesAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("/api/devices", ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<List<DeviceInfo>>(ct) ?? [];
        }
        catch (Exception ex) { _logger.LogError(ex, "GetDevices failed"); return []; }
    }

    private async Task<TOut?> PostJsonAsync<TIn, TOut>(string url, TIn body, CancellationToken ct)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync(url, body, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<TOut>(ct);
        }
        catch (Exception ex) { _logger.LogError(ex, "POST {Url} failed", url); return default; }
    }
}
