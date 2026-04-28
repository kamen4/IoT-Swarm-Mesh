using System.Collections.Concurrent;
using Common.Enums;

namespace BusinessServer.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IUserService"/>.
/// Manages registration, invitation, and role assignment for Telegram bot users.
/// The first user to ever register is automatically promoted to the Admin role.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly ConcurrentDictionary<long, AppUser> _users = new();
    private readonly ConcurrentDictionary<long, string> _invited = new();
    /// <summary>Username-keyed invite list (stored lowercase, without @). Checked on registration when no ID-based invite exists.</summary>
    private readonly ConcurrentDictionary<string, bool> _invitedByUsername = new();
    private readonly object _firstUserLock = new();
    private bool _hasAdmin;

    /// <inheritdoc/>
    public AppUser? GetUser(long telegramId) =>
        _users.TryGetValue(telegramId, out var u) ? u : null;

    /// <inheritdoc/>
    public IReadOnlyList<AppUser> GetAll() => _users.Values.ToList();

    /// <inheritdoc/>
    public (AppUser user, bool isNew) RegisterOrGet(long telegramId, string username)
    {
        if (_users.TryGetValue(telegramId, out var existing))
            return (existing, false);

        UserRole role;
        lock (_firstUserLock)
        {
            role = _hasAdmin ? UserRole.User : UserRole.Admin;
            if (!_hasAdmin) _hasAdmin = true;
        }

        var user = new AppUser(telegramId, username, role);
        _users[telegramId] = user;
        _invited.TryRemove(telegramId, out _);
        // Also clear any matching username-based invite.
        if (!string.IsNullOrEmpty(username))
            _invitedByUsername.TryRemove(username.ToLowerInvariant(), out _);
        return (user, true);
    }

    /// <inheritdoc/>
    public void Invite(long telegramId, string username) =>
        _invited[telegramId] = username;

    /// <inheritdoc/>
    public bool IsInvited(long telegramId) =>
        _invited.ContainsKey(telegramId);

    /// <inheritdoc/>
    public void InviteByUsername(string username) =>
        _invitedByUsername[username.ToLowerInvariant()] = true;

    /// <inheritdoc/>
    public bool IsInvitedByUsername(string username) =>
        _invitedByUsername.ContainsKey(username.ToLowerInvariant());

    /// <inheritdoc/>
    public void SetRole(long telegramId, UserRole role)
    {
        if (!_users.TryGetValue(telegramId, out var u)) return;
        _users[telegramId] = u with { Role = role };
    }

    /// <inheritdoc/>
    public void Remove(long telegramId)
    {
        _users.TryRemove(telegramId, out _);
        _invited.TryRemove(telegramId, out _);
    }
}
