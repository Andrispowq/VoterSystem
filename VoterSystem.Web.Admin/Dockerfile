FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80

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

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
WORKDIR /wwwroot

FROM nginx:alpine
COPY ../../nginx/admin-nginx.conf /etc/nginx/admin-nginx.conf
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html/
