FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80

ARG API_HTTP
ARG API_HTTPS
ARG WEB_HTTP
ARG WEB_HTTPS
ARG ADMIN_HTTP
ARG ADMIN_HTTPS

ENV API_HTTP=$API_HTTP
ENV API_HTTPS=$API_HTTPS
ENV WEB_HTTP=$WEB_HTTP
ENV WEB_HTTPS=$WEB_HTTPS
ENV ADMIN_HTTP=$ADMIN_HTTP
ENV ADMIN_HTTPS=$ADMIN_HTTPS

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VoterSystem.Web.Admin/VoterSystem.Web.Admin.csproj", "VoterSystem.Web.Admin/"]
COPY ["VoterSystem.Shared.Blazor/VoterSystem.Shared.Blazor.csproj", "VoterSystem.Shared.Blazor/"]
COPY ["VoterSystem.Shared/VoterSystem.Shared.csproj", "VoterSystem.Shared/"]
RUN dotnet restore "VoterSystem.Web.Admin/VoterSystem.Web.Admin.csproj"
COPY . .
WORKDIR "/src/VoterSystem.Web.Admin"
RUN dotnet build "VoterSystem.Web.Admin.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "VoterSystem.Web.Admin.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 2. FINAL STAGE: Move to the publish directory and do the replacement with envsubst
FROM base AS final
WORKDIR /app

# Copy the published files from the publish stage
COPY --from=publish /app/publish . 

# Move to the directory where the appsettings.json is located and replace with envsubst
WORKDIR /app/wwwroot

# Copy appsettings.temp.json to appsettings.json in the final image
COPY --from=publish /app/publish/wwwroot/appsettings.temp.json /app/wwwroot/appsettings.json

# Install gettext for envsubst and do the replacement
RUN apt-get update && apt-get install -y gettext && \
    envsubst < ./appsettings.json > ./appsettings.json.tmp && \
    mv ./appsettings.json.tmp ./appsettings.json

FROM nginx:alpine
COPY ../../nginx/nginx.conf /etc/nginx/nginx.conf
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html/

