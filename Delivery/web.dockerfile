# Multi-stage build for ARM32 Blazor WebAssembly optimization
# Stage 1: Build Blazor WASM application
FROM --platform=linux/arm/v7 mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

# Set working directory
WORKDIR /src

# Install necessary packages for ARM32 builds and WASM tools
RUN apk add --no-cache \
    git \
    bash \
    curl \
    nodejs \
    npm \
    && rm -rf /var/cache/apk/*

# Install .NET WASM tools
RUN dotnet workload install wasm-tools

# Copy solution and project files first
COPY FileCategorization.sln .
COPY FileCategorization_Web/FileCategorization_Web.csproj ./FileCategorization_Web/
COPY FileCategorization_Shared/FileCategorization_Shared.csproj ./FileCategorization_Shared/

# Restore packages
RUN dotnet restore

# Copy the rest of the source code
COPY FileCategorization_Web/ ./FileCategorization_Web/
COPY FileCategorization_Shared/ ./FileCategorization_Shared/

# Build and publish Blazor WASM with optimizations
WORKDIR /src/FileCategorization_Web
RUN dotnet publish \
    --configuration Release \
    --output /app/publish \
    --verbosity minimal \
    /p:PublishTrimmed=true \
    /p:BlazorEnableCompression=true \
    /p:BlazorEnableTimeZoneSupport=false \
    /p:InvariantGlobalization=true

# Stage 2: Nginx runtime environment (ARM32)
FROM --platform=linux/arm/v7 nginx:alpine AS runtime

# Install additional packages for better performance
RUN apk add --no-cache \
    curl \
    tzdata \
    && rm -rf /var/cache/apk/*

# Set timezone
ENV TZ=Europe/Rome

# Remove default nginx configuration
RUN rm /etc/nginx/conf.d/default.conf

# Copy optimized nginx configuration
COPY Delivery/nginx.conf /etc/nginx/nginx.conf
COPY Delivery/nginx-site.conf /etc/nginx/conf.d/default.conf

# Copy Blazor WASM files from build stage
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Create nginx user and set permissions
RUN chown -R nginx:nginx /usr/share/nginx/html && \
    chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    chown -R nginx:nginx /etc/nginx/conf.d

# Switch to nginx user
USER nginx

# Optimize static files with compression
RUN find /usr/share/nginx/html -type f \( -name "*.js" -o -name "*.css" -o -name "*.html" -o -name "*.json" \) \
    -exec gzip -k -9 {} \;

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:80/ || exit 1

# Expose ports
EXPOSE 80 443

# Start nginx
CMD ["nginx", "-g", "daemon off;"]

# ARM32 specific optimizations metadata
LABEL org.opencontainers.image.title="FileCategorization Web" \
      org.opencontainers.image.description="File categorization Web UI optimized for ARM32 QNAP NAS" \
      org.opencontainers.image.architecture="arm" \
      org.opencontainers.image.variant="v7"