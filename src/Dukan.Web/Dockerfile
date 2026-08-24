# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy central build files first (cached layer for restore)
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY Dukan.slnx ./

# Copy project file and restore (layer cache)
COPY src/Dukan.Web/Dukan.Web.csproj src/Dukan.Web/
RUN dotnet restore src/Dukan.Web/Dukan.Web.csproj

# Copy everything and build
COPY . .
RUN dotnet publish src/Dukan.Web/Dukan.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for healthcheck (as root, then switch back)
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER app

COPY --from=build /app/publish .

# Environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Dukan.Web.dll"]
