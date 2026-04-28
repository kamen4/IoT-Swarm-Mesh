using Common.Enums;

namespace BusinessServer.Services;

/// <summary>Represents a registered Telegram bot user with their assigned privilege role.</summary>
public record AppUser(long TelegramId, string Username, UserRole Role);

/// <summary>Defines the contract for managing Telegram bot user registration, invitation, and role assignment.</summary>
public interface IUserService
{
    /// <summary>Returns the registered user with the given Telegram ID, or <see langword="null"/> if the user has not yet registered.</summary>
    AppUser? GetUser(long telegramId);

    /// <summary>Returns a snapshot of all currently registered users.</summary>
    IReadOnlyList<AppUser> GetAll();

    /// <summary>
    /// Called on first /start or when an invited user sends any message.
    /// Returns (user, isNew).  The very first registration ever becomes Admin.
    /// </summary>
    (AppUser user, bool isNew) RegisterOrGet(long telegramId, string username);

    /// <summary>Pre-invites a Telegram user by ID so they can self-register on their next contact with the bot.</summary>
    void Invite(long telegramId, string username);

    /// <summary>Returns <see langword="true"/> if the given Telegram user has been invited but has not yet registered.</summary>
    bool IsInvited(long telegramId);

    /// <summary>Pre-invites a Telegram user by their @username (without the @ prefix) so they can self-register on their next contact with the bot.</summary>
    void InviteByUsername(string username);

    /// <summary>Returns <see langword="true"/> if a user with the given username has been invited but has not yet registered.</summary>
    bool IsInvitedByUsername(string username);

    /// <summary>
    /// Replaces the role of an existing registered user. Has no effect if the user is not found.
    /// </summary>
    void SetRole(long telegramId, UserRole role);

    /// <summary>Removes a user from both the registered users list and the invitation list. Has no effect if the user is not found.</summary>
    void Remove(long telegramId);
}
