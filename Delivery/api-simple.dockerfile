# Simplified ARM32 Dockerfile for better compatibility
# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory
WORKDIR /src

# Copy solution and project files first (better caching)
COPY FileCategorization.sln .
COPY FileCategorization_Api/FileCategorization_Api.csproj ./FileCategorization_Api/
COPY FileCategorization_Shared/FileCategorization_Shared.csproj ./FileCategorization_Shared/

# Restore packages (no runtime specification for compatibility)
RUN dotnet restore

# Copy the rest of the source code
COPY FileCategorization_Api/ ./FileCategorization_Api/
COPY FileCategorization_Shared/ ./FileCategorization_Shared/

# Build and publish (let Docker handle the architecture)
WORKDIR /src/FileCategorization_Api
RUN dotnet publish \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Stage 2: Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set environment variables for optimal performance
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    ASPNETCORE_URLS=http://+:8080

# Set working directory
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Create necessary directories
RUN mkdir -p /data /data/Log

# Expose port
EXPOSE 8080

# Set entry point
ENTRYPOINT ["dotnet", "FileCategorization_Api.dll"]