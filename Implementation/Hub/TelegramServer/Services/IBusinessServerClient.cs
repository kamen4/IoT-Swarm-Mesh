using Common.Dto;
using Common.Enums;

namespace TelegramServer.Services;

/// <summary>
/// HTTP client contract for the BusinessServer API.
/// Covers user management and device commands.
/// TelegramServer calls this interface for all business operations.
/// </summary>
public interface IBusinessServerClient
{
    /// <summary>
    /// Registers the Telegram user in BusinessServer.
    /// The very first caller ever becomes Admin; subsequent callers must be invited first.
    /// Returns null if the server is unreachable.
    /// </summary>
    Task<UserDto?> RegisterUserAsync(long telegramId, string username, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a registered user by Telegram ID.
    /// Returns null if the user is not registered.
    /// </summary>
    Task<UserDto?> GetUserAsync(long telegramId, CancellationToken ct = default);

    /// <summary>Returns all registered users from BusinessServer.</summary>
    Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Invites a user by Telegram ID so they will be auto-registered on next contact.
    /// </summary>
    Task InviteUserAsync(long telegramId, string username, CancellationToken ct = default);

    /// <summary>
    /// Invites a user by @username (without the @ prefix) so they will be auto-registered on next contact.
    /// </summary>
    Task InviteUserByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>Updates the role of an existing registered user.</summary>
    Task SetRoleAsync(long telegramId, UserRole role, CancellationToken ct = default);

    /// <summary>Removes a user from the registry.</summary>
    Task RemoveUserAsync(long telegramId, CancellationToken ct = default);

    /// <summary>
    /// Sends a pin toggle command to the gateway device via BusinessServer.
    /// Returns null if the device does not respond within the server-side timeout.
    /// </summary>
    Task<PinToggleResponse?> TogglePinAsync(int pin, CancellationToken ct = default);

    /// <summary>Returns the list of devices registered in BusinessServer.</summary>
    Task<List<Common.Entities.DeviceInfo>> GetDevicesAsync(CancellationToken ct = default);
}
