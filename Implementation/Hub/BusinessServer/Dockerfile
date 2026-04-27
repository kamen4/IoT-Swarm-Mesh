FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["BusinessServer/BusinessServer.csproj", "BusinessServer/"]
RUN dotnet restore "BusinessServer/BusinessServer.csproj"

COPY Common/ Common/
COPY BusinessServer/ BusinessServer/
WORKDIR "/src/BusinessServer"
RUN dotnet build "BusinessServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BusinessServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BusinessServer.dll"]
