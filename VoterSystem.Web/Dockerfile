FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80

ARGS API_HTTP
ARGS WEB_HTTP
ARGS ADMIN_HTTP

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VoterSystem.Web/VoterSystem.Web.csproj", "VoterSystem.Web/"]
COPY ["VoterSystem.Shared.Blazor/VoterSystem.Shared.Blazor.csproj", "VoterSystem.Shared.Blazor/"]
COPY ["VoterSystem.Shared/VoterSystem.Shared.csproj", "VoterSystem.Shared/"]
RUN dotnet restore "VoterSystem.Web/VoterSystem.Web.csproj"
COPY . .
WORKDIR "/src/VoterSystem.Web"
RUN dotnet build "VoterSystem.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "VoterSystem.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
WORKDIR /app

# Copy and process config
COPY ./wwwroot/config.template.json ./wwwroot/config.json

# Replace with env value using envsubst
RUN apt-get update && apt-get install -y gettext && \
    envsubst < ./wwwroot/config.json > ./wwwroot/config.json.tmp && \
    mv ./wwwroot/config.json.tmp ./wwwroot/config.json

FROM nginx:alpine
COPY ../../nginx/web-nginx.conf /etc/nginx/web-nginx.conf
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html/
