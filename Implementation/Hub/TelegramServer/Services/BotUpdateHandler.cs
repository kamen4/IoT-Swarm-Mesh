using System.Collections.Concurrent;
using System.Text;
using Common.Dto;
using Common.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramServer.Services;

/// <summary>
/// Processes all Telegram updates. Business logic is intentionally absent --
/// every decision is delegated to BusinessServer via IBusinessServerClient.
/// Navigation callbacks edit the existing message in place; slash commands and
/// first contacts send new messages.
/// </summary>
public sealed class BotUpdateHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly IBusinessServerClient _api;
    private readonly ILogger<BotUpdateHandler> _logger;

    /// <summary>
    /// Tracks users that are in a multi-step input flow (e.g. waiting for an ID to invite or remove).
    /// Key = caller TelegramId. Stores the action token and the bot message to edit with the outcome.
    /// </summary>
    private readonly ConcurrentDictionary<long, PendingState> _pendingAction = new();

    /// <summary>Pending-flow state: which action is awaited and which bot message to update with the result.</summary>
    private sealed record PendingState(string Action, long ChatId, int MsgId);

    /// <summary>Token stored in PendingState.Action while waiting for a Telegram ID or @username to invite.</summary>
    private const string PendingInvite = "await:invite";

    /// <summary>Token stored in PendingState.Action while waiting for a Telegram ID to remove.</summary>
    private const string PendingRemove = "await:remove";

    /// <summary>
    /// Initialises the handler with the Telegram client, BusinessServer HTTP client, and logger.
    /// </summary>
    public BotUpdateHandler(
        ITelegramBotClient bot,
        IBusinessServerClient api,
        ILogger<BotUpdateHandler> logger)
    {
        _bot = bot;
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// Dispatches an incoming Telegram update to the appropriate handler.
    /// Message updates are routed to HandleMessageAsync; CallbackQuery updates to HandleCallbackAsync.
    /// </summary>
    public async Task HandleUpdateAsync(Update update, CancellationToken ct)
    {
        if (update.Type == UpdateType.Message && update.Message is { } msg)
            await HandleMessageAsync(msg, ct);
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is { } cb)
            await HandleCallbackAsync(cb, ct);
    }

    /// <summary>Logs the polling error. Called by the Telegram polling infrastructure on transient failures.</summary>
    public Task HandleErrorAsync(Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Telegram polling error");
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Message handler
    // -------------------------------------------------------------------------
    /// <summary>
    /// Routes an incoming text message. If the user is mid-flow the input is forwarded
    /// to that flow. /start triggers registration. Other slash commands are dispatched.
    /// Any other text opens the main menu as a new message.
    /// </summary>
    private async Task HandleMessageAsync(Message msg, CancellationToken ct)
    {
        var senderId = msg.From?.Id ?? 0;
        if (senderId == 0) return;

        var senderName = msg.From?.Username ?? msg.From?.FirstName ?? "unknown";
        var text = msg.Text?.Trim() ?? string.Empty;

        _logger.LogInformation("Message from {Id} (@{Name}): {Text}", senderId, senderName, text);

        // If the user is mid-flow, forward input to that flow.
        if (_pendingAction.TryGetValue(senderId, out var pending))
        {
            await HandlePendingFlowAsync(senderId, pending, text, ct);
            return;
        }

        // Registration / /start
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            await HandleStartAsync(senderId, senderName, msg.Chat.Id, ct);
            return;
        }

        // Auto-register invited users on first contact.
        var user = await _api.GetUserAsync(senderId, ct);
        if (user is null)
        {
            user = await _api.RegisterUserAsync(senderId, senderName, ct);
        }

        if (user is null)
        {
            await _bot.SendMessage(msg.Chat.Id,
                "You are not registered. Contact the bot admin to be invited.",
                cancellationToken: ct);
            return;
        }

        if (text.StartsWith('/'))
            await HandleSlashCommandAsync(user, msg.Chat.Id, text, ct);
        else
            await RenderMainMenuAsync(msg.Chat.Id, null, user, null, ct);
    }

    // -------------------------------------------------------------------------
    // /start
    // -------------------------------------------------------------------------
    /// <summary>
    /// Handles /start. Registers the caller via BusinessServer (first caller becomes Admin).
    /// Sends a greeting then renders the main menu as a new message.
    /// </summary>
    private async Task HandleStartAsync(long telegramId, string username, long chatId, CancellationToken ct)
    {
        var user = await _api.RegisterUserAsync(telegramId, username, ct);
        if (user is null)
        {
            await _bot.SendMessage(chatId,
                "Failed to connect to the server. Please try again later.",
                cancellationToken: ct);
            return;
        }

        var greeting = user.Role == UserRole.Admin
            ? $"Welcome, Admin @{username}!"
            : $"Welcome, @{username}! Role: {RoleName(user.Role)}.";

        await _bot.SendMessage(chatId, greeting, cancellationToken: ct);
        await RenderMainMenuAsync(chatId, null, user, null, ct);
    }

    // -------------------------------------------------------------------------
    // Slash command shortcuts
    // -------------------------------------------------------------------------
    /// <summary>
    /// Handles slash commands sent as text messages (/menu, /pin, /toggle, /users, /devices).
    /// Unknown commands fall back to the main menu. All send new messages (no existing message to edit).
    /// </summary>
    private async Task HandleSlashCommandAsync(UserDto user, long chatId, string text, CancellationToken ct)
    {
        switch (text.Split(' ')[0].ToLowerInvariant())
        {
            case "/menu":
                await RenderMainMenuAsync(chatId, null, user, null, ct);
                break;
            case "/pin":
            case "/toggle":
                await HandleTogglePin8Async(chatId, null, user, ct);
                break;
            case "/users":
                await RequireRole(user, UserRole.DedicatedAdmin, chatId, ct,
                    () => RenderUsersMenuAsync(chatId, null, user.Role, null, ct));
                break;
            case "/devices":
                await RenderDevicesAsync(chatId, null, ct);
                break;
            default:
                await RenderMainMenuAsync(chatId, null, user, null, ct);
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Callback query handler
    // -------------------------------------------------------------------------
    /// <summary>
    /// Handles inline keyboard button presses. All navigation edits the existing message in place.
    /// Grant/revoke DA requires Admin role and refreshes the users list in the same message.
    /// Navigation callbacks also clear any stale pending input state.
    /// </summary>
    private async Task HandleCallbackAsync(CallbackQuery cb, CancellationToken ct)
    {
        var senderId = cb.From.Id;
        var senderName = cb.From.Username ?? cb.From.FirstName;
        var chatId = cb.Message?.Chat.Id ?? 0;
        var msgId = cb.Message?.MessageId ?? 0;
        var data = cb.Data ?? string.Empty;

        await _bot.AnswerCallbackQuery(cb.Id, cancellationToken: ct);

        var user = await _api.GetUserAsync(senderId, ct);
        if (user is null)
        {
            await _bot.SendMessage(chatId, "You are not registered.", cancellationToken: ct);
            return;
        }

        _logger.LogInformation("Callback from {Id} (@{Name}): {Data}", senderId, senderName, data);

        if (data == MenuBuilder.CbMenu)
        {
            _pendingAction.TryRemove(senderId, out _);
            await RenderMainMenuAsync(chatId, msgId, user, null, ct);
            return;
        }

        if (data == MenuBuilder.CbDevices)
        {
            _pendingAction.TryRemove(senderId, out _);
            await RenderDevicesAsync(chatId, msgId, ct);
            return;
        }

        if (data == MenuBuilder.CbTogglePin8)
        {
            await HandleTogglePin8Async(chatId, msgId, user, ct);
            return;
        }

        if (data == MenuBuilder.CbUsers)
        {
            _pendingAction.TryRemove(senderId, out _);
            await RequireRole(user, UserRole.DedicatedAdmin, chatId, ct,
                () => RenderUsersMenuAsync(chatId, msgId, user.Role, null, ct));
            return;
        }

        if (data == MenuBuilder.CbUsersList)
        {
            _pendingAction.TryRemove(senderId, out _);
            await RequireRole(user, UserRole.DedicatedAdmin, chatId, ct,
                () => RenderUsersListAsync(chatId, msgId, user.Role, ct));
            return;
        }

        if (data == MenuBuilder.CbUsersAdd)
        {
            await RequireRole(user, UserRole.DedicatedAdmin, chatId, ct, async () =>
            {
                _pendingAction[senderId] = new PendingState(PendingInvite, chatId, msgId);
                await RenderInvitePromptAsync(chatId, msgId, ct);
            });
            return;
        }

        if (data == MenuBuilder.CbUsersRemove)
        {
            await RequireRole(user, UserRole.DedicatedAdmin, chatId, ct, async () =>
            {
                _pendingAction[senderId] = new PendingState(PendingRemove, chatId, msgId);
                await RenderRemovePromptAsync(chatId, msgId, ct);
            });
            return;
        }

        if (data == MenuBuilder.CbUsersCancel)
        {
            _pendingAction.TryRemove(senderId, out _);
            await RequireRole(user, UserRole.DedicatedAdmin, chatId, ct,
                () => RenderUsersMenuAsync(chatId, msgId, user.Role, null, ct));
            return;
        }

        if (data.StartsWith(MenuBuilder.CbGrantDa))
        {
            await RequireRole(user, UserRole.Admin, chatId, ct, async () =>
            {
                if (long.TryParse(data[MenuBuilder.CbGrantDa.Length..], out var tid))
                {
                    await _api.SetRoleAsync(tid, UserRole.DedicatedAdmin, ct);
                    await RenderUsersListAsync(chatId, msgId, user.Role, ct);
                }
            });
            return;
        }

        if (data.StartsWith(MenuBuilder.CbRevokeDa))
        {
            await RequireRole(user, UserRole.Admin, chatId, ct, async () =>
            {
                if (long.TryParse(data[MenuBuilder.CbRevokeDa.Length..], out var tid))
                {
                    await _api.SetRoleAsync(tid, UserRole.User, ct);
                    await RenderUsersListAsync(chatId, msgId, user.Role, ct);
                }
            });
        }
    }

    // -------------------------------------------------------------------------
    // Pending multi-step flows
    // -------------------------------------------------------------------------
    /// <summary>
    /// Handles user text input during a multi-step flow (invite or remove).
    /// Clears the pending state, processes the input, then edits the stored bot message with the result.
    /// </summary>
    private async Task HandlePendingFlowAsync(long senderId, PendingState state, string input, CancellationToken ct)
    {
        _pendingAction.TryRemove(senderId, out _);

        var user = await _api.GetUserAsync(senderId, ct);
        if (user is null) return;

        int? msgId = state.MsgId > 0 ? state.MsgId : null;

        if (state.Action == PendingInvite)
        {
            var trimmed = input.Trim();
            string resultMsg;
            if (trimmed.StartsWith('@'))
            {
                var usernameToInvite = trimmed[1..];
                if (string.IsNullOrEmpty(usernameToInvite))
                {
                    resultMsg = "Invalid username. Operation cancelled.";
                }
                else
                {
                    await _api.InviteUserByUsernameAsync(usernameToInvite, ct);
                    resultMsg = $"@{usernameToInvite} invited. They will be registered on first contact.";
                }
            }
            else if (long.TryParse(trimmed, out var targetId))
            {
                await _api.InviteUserAsync(targetId, string.Empty, ct);
                resultMsg = $"User {targetId} invited. They will be registered on first contact.";
            }
            else
            {
                resultMsg = "Invalid input. Expected a numeric ID or @username.";
            }
            await RenderUsersMenuAsync(state.ChatId, msgId, user.Role, resultMsg, ct);
            return;
        }

        if (state.Action == PendingRemove)
        {
            string resultMsg;
            if (!long.TryParse(input.Trim(), out var targetId))
            {
                resultMsg = "Invalid ID. Operation cancelled.";
            }
            else
            {
                await _api.RemoveUserAsync(targetId, ct);
                resultMsg = $"User {targetId} removed.";
            }
            await RenderUsersMenuAsync(state.ChatId, msgId, user.Role, resultMsg, ct);
        }
    }

    // -------------------------------------------------------------------------
    // Render helpers -- edit existing message or send new
    // -------------------------------------------------------------------------
    /// <summary>
    /// Core render helper. Edits the specified message when editMsgId is non-null;
    /// falls back to sending a new message on failure or when editMsgId is null.
    /// Swallows "message is not modified" errors silently.
    /// </summary>
    private async Task RenderAsync(long chatId, int? editMsgId, string text, InlineKeyboardMarkup markup, CancellationToken ct)
    {
        if (editMsgId.HasValue)
        {
            try
            {
                await _bot.EditMessageText(chatId, editMsgId.Value, text,
                    replyMarkup: markup, cancellationToken: ct);
                return;
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EditMessageText failed, falling back to SendMessage");
            }
        }
        await _bot.SendMessage(chatId, text, replyMarkup: markup, cancellationToken: ct);
    }

    /// <summary>Renders the main menu. An optional status line is shown below the role.</summary>
    private async Task RenderMainMenuAsync(long chatId, int? editMsgId, UserDto user, string? statusLine, CancellationToken ct)
    {
        var sb = new StringBuilder(MenuBuilder.BcMain);
        sb.Append("\n\nRole: ");
        sb.Append(RoleName(user.Role));
        if (!string.IsNullOrEmpty(statusLine))
        {
            sb.Append("\n\n");
            sb.Append(statusLine);
        }
        await RenderAsync(chatId, editMsgId, sb.ToString(), MenuBuilder.MainMenu(user.Role), ct);
    }

    /// <summary>Renders the users submenu. An optional status line is shown below the header.</summary>
    private async Task RenderUsersMenuAsync(long chatId, int? editMsgId, UserRole callerRole, string? statusLine, CancellationToken ct)
    {
        var sb = new StringBuilder(MenuBuilder.BcUsers);
        if (!string.IsNullOrEmpty(statusLine))
        {
            sb.Append("\n\n");
            sb.Append(statusLine);
        }
        await RenderAsync(chatId, editMsgId, sb.ToString(), MenuBuilder.UsersMenu(callerRole), ct);
    }

    /// <summary>Fetches device list and renders the devices screen.</summary>
    private async Task RenderDevicesAsync(long chatId, int? editMsgId, CancellationToken ct)
    {
        var devices = await _api.GetDevicesAsync(ct);
        var sb = new StringBuilder(MenuBuilder.BcDevices);
        sb.Append("\n\n");
        if (devices.Count == 0)
            sb.Append("No devices registered.");
        else
            sb.Append(string.Join('\n', devices.Select(d =>
                $"  {d.DeviceId} ({d.DeviceType}) -- {(d.IsOnline ? "online" : "offline")}")));
        await RenderAsync(chatId, editMsgId, sb.ToString(), MenuBuilder.DevicesMenu(), ct);
    }

    /// <summary>
    /// Fetches the user list and renders the users list screen.
    /// Admin callers see +DA / -DA action buttons per user.
    /// </summary>
    private async Task RenderUsersListAsync(long chatId, int? editMsgId, UserRole callerRole, CancellationToken ct)
    {
        var users = await _api.GetAllUsersAsync(ct);
        var sb = new StringBuilder(MenuBuilder.BcUsersList);
        sb.Append("\n\n");
        if (users.Count == 0)
            sb.Append("No users registered.");
        else
            sb.Append(string.Join('\n', users.Select(u =>
                $"  @{u.Username} ({u.TelegramId}) -- {RoleName(u.Role)}")));

        var rows = new List<InlineKeyboardButton[]>();
        foreach (var u in users)
        {
            if (callerRole == UserRole.Admin && u.Role < UserRole.DedicatedAdmin)
                rows.Add([InlineKeyboardButton.WithCallbackData(
                    $"+DA @{u.Username}", MenuBuilder.CbGrantDa + u.TelegramId)]);
            else if (callerRole == UserRole.Admin && u.Role == UserRole.DedicatedAdmin)
                rows.Add([InlineKeyboardButton.WithCallbackData(
                    $"-DA @{u.Username}", MenuBuilder.CbRevokeDa + u.TelegramId)]);
        }
        rows.Add([
            InlineKeyboardButton.WithCallbackData("Refresh", MenuBuilder.CbUsersList),
            InlineKeyboardButton.WithCallbackData("< Back", MenuBuilder.CbUsers),
        ]);
        await RenderAsync(chatId, editMsgId, sb.ToString(), new InlineKeyboardMarkup(rows), ct);
    }

    /// <summary>Renders the invite-user input prompt screen.</summary>
    private async Task RenderInvitePromptAsync(long chatId, int? editMsgId, CancellationToken ct)
    {
        var text = MenuBuilder.BcUsersInvite
            + "\n\nType the Telegram ID (e.g. 123456789) or @username,"
            + "\nthen send it as a message in this chat.";
        await RenderAsync(chatId, editMsgId, text, MenuBuilder.CancelMenu(), ct);
    }

    /// <summary>Renders the remove-user input prompt screen.</summary>
    private async Task RenderRemovePromptAsync(long chatId, int? editMsgId, CancellationToken ct)
    {
        var text = MenuBuilder.BcUsersRemovePrompt
            + "\n\nType the Telegram ID to remove,"
            + "\nthen send it as a message in this chat.";
        await RenderAsync(chatId, editMsgId, text, MenuBuilder.CancelMenu(), ct);
    }

    /// <summary>
    /// Toggles pin 8 via BusinessServer and renders the main menu with the result.
    /// Edits the message in place when editMsgId is provided.
    /// </summary>
    private async Task HandleTogglePin8Async(long chatId, int? editMsgId, UserDto user, CancellationToken ct)
    {
        var resp = await _api.TogglePinAsync(8, ct);
        var statusLine = resp is null
            ? "Pin 8: gateway did not respond."
            : $"Pin 8 -- {(resp.State == 1 ? "HIGH" : "LOW")}";
        await RenderMainMenuAsync(chatId, editMsgId, user, statusLine, ct);
    }

    // -------------------------------------------------------------------------
    // Utilities
    // -------------------------------------------------------------------------
    /// <summary>
    /// Executes the action only if the user's role meets the required minimum.
    /// Silently no-ops when the role is insufficient.
    /// </summary>
    private static async Task RequireRole(
        UserDto user, UserRole required, long chatId,
        CancellationToken ct, Func<Task> action)
    {
        if (user.Role >= required)
            await action();
    }

    /// <summary>Returns the display name for a role value.</summary>
    private static string RoleName(UserRole r) => r switch
    {
        UserRole.Admin => "Admin",
        UserRole.DedicatedAdmin => "Dedicated Admin",
        _ => "User",
    };
}
