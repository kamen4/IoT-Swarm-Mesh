# Properties

## Purpose and Boundary

IDE and CLI launch profiles for local development of TelegramServer.
These files configure how `dotnet run` and IDE debuggers start the application.

## Files

| File                | Purpose                                                                                     |
| ------------------- | ------------------------------------------------------------------------------------------- |
| launchSettings.json | Defines named run profiles (HTTP, HTTPS) with ports, environment variables, and launch URLs |

## Interactions and Constraints

- Not deployed to production; ignored by Docker builds.
- Used only by `dotnet run` and IDE debugger (Visual Studio, Rider, VS Code).
- Environment variables set here (e.g., ASPNETCORE_ENVIRONMENT=Development) override nothing in production containers.
- Do not store secrets, bot tokens, or production URLs in this file.

## Relation to Parent Folder

Sits inside TelegramServer.
The .NET SDK reads this folder automatically when running `dotnet run` from the project directory.
Has no effect on the published output or Docker image.
