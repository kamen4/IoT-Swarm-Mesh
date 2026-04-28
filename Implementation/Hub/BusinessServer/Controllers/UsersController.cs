using BusinessServer.Services;
using Common.Dto;
using Common.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BusinessServer.Controllers;

/// <summary>HTTP API controller that exposes user management operations consumed primarily by TelegramServer.</summary>
[ApiController, Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    /// <summary>Initializes a new instance of <see cref="UsersController"/> with the required user service.</summary>
    public UsersController(IUserService users) => _users = users;

    /// <summary>
    /// Called by TelegramServer when a user sends any message.
    /// Registers the user if they were previously invited or if no admin exists yet; otherwise retrieves the existing record.
    /// The very first registration ever is automatically promoted to Admin.
    /// </summary>
    /// <param name="req">Telegram identity and display name of the user initiating contact.</param>
    /// <returns>The registered or existing user record.</returns>
    [HttpPost("register")]
    public ActionResult<UserDto> Register([FromBody] RegisterUserRequest req)
    {
        var (user, _) = _users.RegisterOrGet(req.TelegramId, req.Username);
        return Ok(ToDto(user));
    }

    /// <summary>
    /// Returns the registered user by Telegram ID, or 404 if the user is not registered.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID to look up.</param>
    [HttpGet("{telegramId:long}")]
    public ActionResult<UserDto> Get(long telegramId)
    {
        var u = _users.GetUser(telegramId);
        return u is null ? NotFound() : Ok(ToDto(u));
    }

    /// <summary>Returns the full list of all registered users.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<UserDto>> GetAll() =>
        Ok(_users.GetAll().Select(ToDto));

    /// <summary>
    /// Pre-invites a Telegram user by ID so they are allowed to self-register on their next contact with the bot.
    /// </summary>
    /// <param name="req">Telegram identity of the user to invite.</param>
    [HttpPost("invite")]
    public IActionResult Invite([FromBody] InviteUserRequest req)
    {
        _users.Invite(req.TelegramId, req.Username);
        return NoContent();
    }

    /// <summary>Pre-invites a Telegram user by @username. The username must be provided without the @ prefix.</summary>
    [HttpPost("invite-by-username")]
    public IActionResult InviteByUsername([FromBody] InviteUserByUsernameRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
            return BadRequest(new { Error = "Username must not be empty." });
        _users.InviteByUsername(req.Username);
        return NoContent();
    }

    /// <summary>
    /// Assigns a new role to an existing user. Returns 404 if the user is not found.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID whose role should be updated.</param>
    /// <param name="req">The new role to assign.</param>
    [HttpPut("{telegramId:long}/role")]
    public IActionResult SetRole(long telegramId, [FromBody] SetRoleRequest req)
    {
        if (_users.GetUser(telegramId) is null) return NotFound();
        _users.SetRole(telegramId, req.Role);
        return NoContent();
    }

    /// <summary>
    /// Removes a user from both the registered users list and the invitation list. Returns 404 if not found.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID to remove.</param>
    [HttpDelete("{telegramId:long}")]
    public IActionResult Remove(long telegramId)
    {
        if (_users.GetUser(telegramId) is null) return NotFound();
        _users.Remove(telegramId);
        return NoContent();
    }

    /// <summary>
    /// Checks whether a user has at least the specified role.
    /// Returns Allowed=true with the user's actual role when the threshold is met.
    /// Returns Allowed=false with a null role when the user is not registered (invited or unknown).
    /// </summary>
    /// <param name="telegramId">The Telegram user ID to check.</param>
    /// <param name="required">Minimum role level required; defaults to <see cref="UserRole.User"/>.</param>
    [HttpGet("{telegramId:long}/access")]
    public ActionResult<CheckAccessResponse> CheckAccess(long telegramId, [FromQuery] UserRole required = UserRole.User)
    {
        var u = _users.GetUser(telegramId);
        if (u is null)
        {
            if (_users.IsInvited(telegramId))
                return Ok(new CheckAccessResponse(false, null));
            return Ok(new CheckAccessResponse(false, null));
        }
        var allowed = u.Role >= required;
        return Ok(new CheckAccessResponse(allowed, u.Role));
    }

    private static UserDto ToDto(AppUser u) => new(u.TelegramId, u.Username, u.Role);
}
