# Services

## Purpose and Boundary

Telegram update handler and HTTP client wrapper for TelegramServer.
This folder contains all classes that process Telegram Bot API updates and communicate with BusinessServer over HTTP.
No business logic lives here; TelegramServer is a pure presentation layer.

## Files

| Interface                | Implementation          | Role                                                                                          |
| ------------------------ | ----------------------- | --------------------------------------------------------------------------------------------- |
| IBusinessServerClient.cs | BusinessServerClient.cs | HttpClient wrapper; exposes typed methods for every BusinessServer API endpoint               |
| --                       | BotUpdateHandler.cs     | Processes Message and CallbackQuery updates from Telegram; dispatches to BusinessServerClient |
| --                       | MenuBuilder.cs          | Builds InlineKeyboardMarkup menus based on the caller's UserRole                              |

## Interactions and Constraints

- No business logic; BotUpdateHandler only translates Telegram events into HTTP calls.
- No Redis access; TelegramServer never touches Redis directly.
- No UART or serial port knowledge.
- BusinessServerClient uses IHttpClientFactory or a named/typed HttpClient registered in Program.cs.
- MenuBuilder is a pure function: given a UserRole it returns a keyboard; no side effects.

## Relation to Parent Folder

BotUpdateHandler is injected into Worker (the hosted BackgroundService in the root of TelegramServer).
IBusinessServerClient is registered as a scoped or singleton service in Program.cs.
MenuBuilder is registered as a singleton or used as a static helper.
