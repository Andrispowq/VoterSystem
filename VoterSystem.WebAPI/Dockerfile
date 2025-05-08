FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 6900

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VoterSystem.WebAPI/VoterSystem.WebAPI.csproj", "VoterSystem.WebAPI/"]
RUN dotnet restore "VoterSystem.WebAPI/VoterSystem.WebAPI.csproj"
COPY . .
WORKDIR "/src/VoterSystem.WebAPI"

RUN sed -ir "s/Host=[^;]*;/Host=db;/g" ./appsettings.json

RUN dotnet build "VoterSystem.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "VoterSystem.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VoterSystem.WebAPI.dll"]
