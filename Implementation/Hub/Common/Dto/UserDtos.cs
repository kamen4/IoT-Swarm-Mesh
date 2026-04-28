using Common.Enums;

namespace Common.Dto;

/// <summary>Represents a registered user as exposed through the HTTP API.</summary>
public record UserDto(long TelegramId, string Username, UserRole Role);

/// <summary>Request body used to register a new user or retrieve an existing one by Telegram identity.</summary>
public record RegisterUserRequest(long TelegramId, string Username);

/// <summary>Request body used to assign a specific role to an existing user.</summary>
public record SetRoleRequest(long TelegramId, UserRole Role);

/// <summary>Request body used to pre-invite a Telegram user so they can self-register on their next contact with the bot.</summary>
public record InviteUserRequest(long TelegramId, string Username);

/// <summary>Request body used to pre-invite a Telegram user by @username so they can self-register on their next contact with the bot.</summary>
public record InviteUserByUsernameRequest(string Username);

/// <summary>Response indicating whether a user meets the required access level, together with their actual role if they are registered.</summary>
public record CheckAccessResponse(bool Allowed, UserRole? Role);
