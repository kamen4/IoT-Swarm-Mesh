# TelegramServer

## Purpose and boundary

TelegramServer is the user-facing front-end of Hub.
It runs a Telegram bot with long polling: receives user messages, maps them to commands, calls
BusinessServer over HTTP, and sends the result back to the Telegram chat.
Boundary: only Telegram Bot API and HTTP client to BusinessServer; device logic and Redis are
outside this service.

## Subfolders

| Folder      | Purpose                                                   |
| ----------- | --------------------------------------------------------- |
| Services/   | Telegram update handler and HTTP client to BusinessServer |
| Properties/ | launchSettings.json for local run                         |

## Files

### Root

| File                         | Responsibility                                                  |
| ---------------------------- | --------------------------------------------------------------- |
| Program.cs                   | Host builder: registers Telegram client, IBusinessServerClient, |
|                              | Worker; reads Telegram:BotToken and BusinessServer:BaseUrl      |
| Worker.cs                    | BackgroundService: starts long polling via Telegram Bot API     |
| TelegramServer.csproj        | .NET project; references Common                                 |
| Dockerfile                   | Multi-stage container build                                     |
| appsettings.json             | Default config: Telegram, BusinessServer base URL               |
| appsettings.Development.json | Overrides for local development                                 |

### Services/

| File                     | Responsibility                                                                    |
| ------------------------ | --------------------------------------------------------------------------------- |
| BotUpdateHandler.cs      | Dispatches all Telegram updates (messages + callback queries);                    |
|                          | enforces role-based menu visibility; delegates all logic to IBusinessServerClient |
| MenuBuilder.cs           | Static helper: builds InlineKeyboardMarkup per user role; defines callback data   |
| IBusinessServerClient.cs | Interface: users CRUD, pin toggle, device list                                    |
| BusinessServerClient.cs  | HTTP client to BusinessServer; wraps all API calls with error handling            |

## Inline keyboard menus

All menus are rendered as Telegram inline keyboards.  The visible buttons depend on the user's role.

| Role           | Main menu buttons                                |
| -------------- | ------------------------------------------------ |
| User           | Devices / Toggle Pin 8                           |
| DedicatedAdmin | + Users (list, invite, remove)                   |
| Admin          | + Grant DA / Revoke DA buttons in the users list |

## Command routing

| Input / callback  | Action                                     | Reply                   |
| ----------------- | ------------------------------------------ | ----------------------- |
| /start            | RegisterUserAsync -> first user = Admin    | Welcome + main menu     |
| /menu             | show main menu                             | inline keyboard         |
| /pin or /toggle   | TogglePinAsync(8)                          | "Pin 8 is now HIGH/LOW" |
| /devices          | GetDevicesAsync                            | device list             |
| /users            | (DedicatedAdmin+) users menu               | inline keyboard         |
| menu:main         | show main menu (callback)                  | inline keyboard         |
| pin:toggle:8      | TogglePinAsync(8) (callback)               | result + main menu      |
| users:list        | GetAllUsersAsync + role buttons (callback) | formatted list          |
| users:add         | prompt for user ID -> InviteUserAsync      | confirmation            |
| users:remove      | prompt for user ID -> RemoveUserAsync      | confirmation            |
| role:grant_da:ID  | SetRoleAsync(DedicatedAdmin) (Admin only)  | confirmation            |
| role:revoke_da:ID | SetRoleAsync(User) (Admin only)            | confirmation            |

No business logic lives in TelegramServer.  All authorization decisions are made by BusinessServer.
OPEN DECISION: move pin number to configuration.

## Configuration

| Key                    | Default                     | Description               |
| ---------------------- | --------------------------- | ------------------------- |
| Telegram:BotToken      | (required, no default)      | Bot token from @BotFather |
| BusinessServer:BaseUrl | http://business-server:8080 | BusinessServer base URL   |

In docker-compose.yml the token is supplied via TELEGRAM_BOT_TOKEN from .env.

## Interactions and constraints

- TelegramServer does not connect to Redis and has no knowledge of UartLS.
- Long polling runs inside Worker; cancelled cleanly on host stop.
- If BusinessServer is unreachable the HTTP request fails; BotUpdateHandler sends an error reply.

## Extension points

- New commands: add a case in BotUpdateHandler, a new method on IBusinessServerClient.
- User authorisation: filter by Telegram user ID in BotUpdateHandler.
- Webhook mode: replace Worker; BotUpdateHandler logic stays unchanged.

## Relation to parent folder

TelegramServer is the sole entry point for user commands into Hub.
It depends on BusinessServer (HTTP) and the Telegram Bot API (external).
Deployed as a container via docker-compose.yml in the hub-net network.
