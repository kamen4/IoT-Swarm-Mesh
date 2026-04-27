# TelegramServer

## Purpose and boundary

TelegramServer is the user-facing front-end of Hub.
It runs a Telegram bot with long polling: receives user messages, maps them to commands, calls
BusinessServer over HTTP, and sends the result back to the Telegram chat.
Boundary: only Telegram Bot API and HTTP client to BusinessServer; device logic and Redis are
outside this service.

## Subfolders

| Folder      | Purpose                                                       |
| ----------- | ------------------------------------------------------------- |
| Services/   | Telegram update handler and HTTP client to BusinessServer     |
| Properties/ | launchSettings.json for local run                             |

## Files

### Root

| File                         | Responsibility                                                       |
| ---------------------------- | -------------------------------------------------------------------- |
| Program.cs                   | Host builder: registers Telegram client, IBusinessServerClient,      |
|                              | Worker; reads Telegram:BotToken and BusinessServer:BaseUrl           |
| Worker.cs                    | BackgroundService: starts long polling via Telegram Bot API          |
| TelegramServer.csproj        | .NET project; references Common                                      |
| Dockerfile                   | Multi-stage container build                                          |
| appsettings.json             | Default config: Telegram, BusinessServer base URL                    |
| appsettings.Development.json | Overrides for local development                                      |

### Services/

| File                     | Responsibility                                                               |
| ------------------------ | ---------------------------------------------------------------------------- |
| BotUpdateHandler.cs      | Dispatches incoming Telegram updates:                                        |
|                          | /toggle or /pin -> TogglePinAsync(pin=8) -> reply "HIGH/LOW"                 |
|                          | any other text  -> EchoAsync(text) -> echo reply                             |
| IBusinessServerClient.cs | Interface contract for BusinessServerClient (eases testing)                  |
| BusinessServerClient.cs  | HTTP client to BusinessServer: POST /api/pin/toggle, POST /api/echo          |

## Command routing

| Input              | Action                                             | Reply                          |
| ------------------ | -------------------------------------------------- | ------------------------------ |
| /toggle            | TogglePinAsync(pin=8) -> POST /api/pin/toggle      | "Gateway pin 8 is now HIGH/LOW"|
| /pin               | TogglePinAsync(pin=8) -> POST /api/pin/toggle      | "Gateway pin 8 is now HIGH/LOW"|
| any other text     | EchoAsync(text)       -> POST /api/echo            | echo text from BusinessServer  |

The pin number (8) is hard-coded in BotUpdateHandler.
OPEN DECISION: move pin number to configuration.

## Configuration

| Key                   | Default                     | Description                   |
| --------------------- | --------------------------- | ----------------------------- |
| Telegram:BotToken     | (required, no default)      | Bot token from @BotFather     |
| BusinessServer:BaseUrl| http://business-server:8080 | BusinessServer base URL       |

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
