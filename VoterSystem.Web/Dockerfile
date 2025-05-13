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

FROM nginx:alpine
COPY ../../nginx/nginx.conf /etc/nginx/nginx.conf
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html/

# Copy file
RUN rm /app/publish/wwwroot/appsettings.json
COPY /app/publish/wwwroot/appsettings.temp.json /app/publish/wwwroot/appsettings.json

# Replace with env value using envsubst
RUN apt-get update && apt-get install -y gettext

CMD envsubst < /app/publish/wwwroot/appsettings.temp.json > /app/publish/wwwroot/appsetting.json.tmp && \
	mv /app/publish/wwwroot/appsettings.json.tmp /app/publish/wwwroot/appsettings.json
