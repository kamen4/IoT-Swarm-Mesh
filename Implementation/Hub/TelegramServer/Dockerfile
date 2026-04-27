FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["TelegramServer/TelegramServer.csproj", "TelegramServer/"]
RUN dotnet restore "TelegramServer/TelegramServer.csproj"

COPY Common/ Common/
COPY TelegramServer/ TelegramServer/
WORKDIR "/src/TelegramServer"
RUN dotnet build "TelegramServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TelegramServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelegramServer.dll"]
