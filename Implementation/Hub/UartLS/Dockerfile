FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Common/Common.csproj Common/
COPY UartLS/UartLS.csproj UartLS/
RUN dotnet restore UartLS/UartLS.csproj
COPY Common/ Common/
COPY UartLS/ UartLS/
RUN dotnet publish UartLS/UartLS.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UartLS.dll"]
