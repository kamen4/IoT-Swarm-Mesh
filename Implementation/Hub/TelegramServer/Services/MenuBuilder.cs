using Common.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramServer.Services;

/// <summary>
/// Builds Telegram inline keyboards and provides breadcrumb text constants for each screen.
/// All callback data tokens and screen header strings are defined here.
/// </summary>
public static class MenuBuilder
{
    // -------------------------------------------------------------------------
    // Callback data tokens
    // -------------------------------------------------------------------------

    /// <summary>Callback data for the main menu screen.</summary>
    public const string CbMenu = "menu:main";
    /// <summary>Callback data for the devices screen (also doubles as Refresh).</summary>
    public const string CbDevices = "menu:devices";
    /// <summary>Callback data for the toggle-pin-8 action.</summary>
    public const string CbTogglePin8 = "pin:toggle:8";
    /// <summary>Callback data for the users submenu (DedicatedAdmin+ only).</summary>
    public const string CbUsers = "menu:users";
    /// <summary>Callback data for the users list screen (also doubles as Refresh).</summary>
    public const string CbUsersList = "users:list";
    /// <summary>Callback data for starting the invite-user input flow.</summary>
    public const string CbUsersAdd = "users:add";
    /// <summary>Callback data for starting the remove-user input flow.</summary>
    public const string CbUsersRemove = "users:remove";
    /// <summary>Callback data for cancelling an active input flow and returning to Users menu.</summary>
    public const string CbUsersCancel = "users:cancel";
    /// <summary>Callback data prefix for granting DedicatedAdmin; target TelegramId is appended.</summary>
    public const string CbGrantDa = "role:grant_da:";
    /// <summary>Callback data prefix for revoking DedicatedAdmin; target TelegramId is appended.</summary>
    public const string CbRevokeDa = "role:revoke_da:";

    // -------------------------------------------------------------------------
    // Breadcrumb header strings
    // -------------------------------------------------------------------------

    /// <summary>Header text for the main menu screen.</summary>
    public const string BcMain = "=== IoT Hub ===";
    /// <summary>Header text for the devices screen.</summary>
    public const string BcDevices = "=== IoT Hub > Devices ===";
    /// <summary>Header text for the users submenu screen.</summary>
    public const string BcUsers = "=== IoT Hub > Users ===";
    /// <summary>Header text for the users list screen.</summary>
    public const string BcUsersList = "=== IoT Hub > Users > List ===";
    /// <summary>Header text for the invite-user input screen.</summary>
    public const string BcUsersInvite = "=== IoT Hub > Users > Invite ===";
    /// <summary>Header text for the remove-user input screen.</summary>
    public const string BcUsersRemovePrompt = "=== IoT Hub > Users > Remove ===";

    // -------------------------------------------------------------------------
    // Keyboard builders
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the main menu keyboard. Devices and Toggle Pin 8 are on one row.
    /// DedicatedAdmin and Admin see an additional Users button.
    /// </summary>
    /// <param name="role">The calling user's role; determines which buttons are shown.</param>
    public static InlineKeyboardMarkup MainMenu(UserRole role)
    {
        var rows = new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Devices", CbDevices),
                InlineKeyboardButton.WithCallbackData("Toggle Pin 8", CbTogglePin8),
            },
        };
        if (role >= UserRole.DedicatedAdmin)
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Users", CbUsers) });
        return new InlineKeyboardMarkup(rows);
    }

    /// <summary>Builds the devices screen keyboard: Refresh and Back on one row.</summary>
    public static InlineKeyboardMarkup DevicesMenu()
        => new(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Refresh", CbDevices),
                InlineKeyboardButton.WithCallbackData("< Back", CbMenu),
            },
        });

    /// <summary>
    /// Builds the users management submenu keyboard (List, Invite + Remove on one row, Back).
    /// </summary>
    /// <param name="callerRole">Reserved for future role-based filtering.</param>
    public static InlineKeyboardMarkup UsersMenu(UserRole callerRole)
    {
        var rows = new List<InlineKeyboardButton[]>
        {
            new[] { InlineKeyboardButton.WithCallbackData("List users", CbUsersList) },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Invite user", CbUsersAdd),
                InlineKeyboardButton.WithCallbackData("Remove user", CbUsersRemove),
            },
            new[] { InlineKeyboardButton.WithCallbackData("< Back", CbMenu) },
        };
        return new InlineKeyboardMarkup(rows);
    }

    /// <summary>Returns a single Cancel button that clears the active input flow and returns to Users menu.</summary>
    public static InlineKeyboardMarkup CancelMenu()
        => new(new[] { new[] { InlineKeyboardButton.WithCallbackData("Cancel", CbUsersCancel) } });
}
