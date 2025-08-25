# Multi-stage build for ARM32 optimization
# Stage 1: Build environment
FROM --platform=linux/arm/v7 mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

# Set working directory
WORKDIR /src

# Install necessary packages for ARM32 builds
RUN apk add --no-cache \
    git \
    bash \
    curl \
    && rm -rf /var/cache/apk/*

# Copy solution and project files first (better caching)
COPY FileCategorization.sln .
COPY FileCategorization_Api/FileCategorization_Api.csproj ./FileCategorization_Api/
COPY FileCategorization_Shared/FileCategorization_Shared.csproj ./FileCategorization_Shared/

# Restore packages (this layer will be cached if no project changes)
RUN dotnet restore --runtime linux-arm

# Copy the rest of the source code
COPY FileCategorization_Api/ ./FileCategorization_Api/
COPY FileCategorization_Shared/ ./FileCategorization_Shared/

# Build and publish with ARM32 optimizations
WORKDIR /src/FileCategorization_Api
RUN dotnet publish \
    --configuration Release \
    --runtime linux-arm \
    --self-contained false \
    --output /app/publish \
    --verbosity minimal \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false \
    /p:EnableCompressionInSingleFile=false

# Stage 2: Runtime environment (ARM32 optimized)
FROM --platform=linux/arm/v7 mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Install additional runtime dependencies for ARM32
RUN apk add --no-cache \
    icu-libs \
    tzdata \
    && rm -rf /var/cache/apk/*

# Set environment variables for optimal ARM32 performance
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_GCServer=0 \
    DOTNET_GCConcurrent=1 \
    DOTNET_GCRetainVM=25 \
    ASPNETCORE_URLS=http://+:5089 \
    TZ=Europe/Rome

# Create non-root user for security
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Set working directory
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Create necessary directories with proper permissions
RUN mkdir -p \
    /data \
    /data/Log \
    /app/Temp \
    /app/Temp/MlData \
    && chown -R appuser:appgroup /data /app

# Copy ML.NET models and training data if they exist
COPY --chown=appuser:appgroup FileCategorization_Api/Temp/ ./Temp/

# Switch to non-root user
USER appuser

# Health check for container monitoring
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5089/health || exit 1

# Expose ports
EXPOSE 5089

# Set entry point
ENTRYPOINT ["dotnet", "FileCategorization_Api.dll"]

# ARM32 specific optimizations metadata
LABEL org.opencontainers.image.title="FileCategorization API" \
      org.opencontainers.image.description="File categorization API optimized for ARM32 QNAP NAS" \
      org.opencontainers.image.architecture="arm" \
      org.opencontainers.image.variant="v7"