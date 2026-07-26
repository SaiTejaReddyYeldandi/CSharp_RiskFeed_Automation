FROM node:22-alpine AS web-build
WORKDIR /web
COPY src/RiskFeed.Web/package.json src/RiskFeed.Web/package-lock.json ./
RUN npm ci
COPY src/RiskFeed.Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
WORKDIR /source
COPY Directory.Build.props Directory.Packages.props global.json RiskFeed.sln ./
COPY src/RiskFeed.Core/RiskFeed.Core.csproj src/RiskFeed.Core/
COPY src/RiskFeed.Infrastructure/RiskFeed.Infrastructure.csproj src/RiskFeed.Infrastructure/
COPY src/RiskFeed.Api/RiskFeed.Api.csproj src/RiskFeed.Api/
RUN dotnet restore src/RiskFeed.Api/RiskFeed.Api.csproj
COPY src/ src/
RUN dotnet publish src/RiskFeed.Api/RiskFeed.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=dotnet-build /app/publish ./
COPY --from=web-build /web/dist ./wwwroot
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RiskFeed.Api.dll"]
