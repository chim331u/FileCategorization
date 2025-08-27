#!/bin/bash

# QNAP ARM32 Web Deployment - Memory Optimized for 1GB RAM
# Enhanced version with memory management and error recovery

set -e

echo "============================================================"
echo "  FileCategorization WEB - ARM32 Memory Optimized Deployment"
echo "============================================================"
echo "Repository: https://github.com/chim331u/FileCategorization.git"
echo "Branch: DeliveryNasArm32"
echo "Optimizations: Memory management, error recovery, ARM32 specific"
echo "============================================================"

# Configuration
REPO_URL="https://github.com/chim331u/FileCategorization.git"
BRANCH="DeliveryNasArm32"
CONTAINER_NAME="filecat_web"
IMAGE_NAME="filecat_web_image:latest"
PORT="30229:80"
API_URL="http://localhost:30219/"
WORK_DIR="/tmp/FileCategorization_ARM32"

# Memory management for ARM32
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_EnableWriteXorExecute=0
export DOTNET_TieredPGO=0

# Clean previous attempts and free memory
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Cleaning previous deployment..."
docker rm -f $CONTAINER_NAME 2>/dev/null || true
docker rmi $IMAGE_NAME 2>/dev/null || true
rm -rf $WORK_DIR

# Free up memory
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Freeing system memory for build..."
docker system prune -f 2>/dev/null || true
sync && echo 3 > /proc/sys/vm/drop_caches 2>/dev/null || true

# Create working directory and download
mkdir -p $WORK_DIR
cd $WORK_DIR

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Downloading repository..."
if command -v git >/dev/null 2>&1; then
    git clone --depth 1 --single-branch --branch $BRANCH $REPO_URL repo
else
    ZIP_URL="https://github.com/chim331u/FileCategorization/archive/refs/heads/$BRANCH.zip"
    wget -O repo.zip "$ZIP_URL" 
    unzip -q repo.zip
    mv FileCategorization-$BRANCH repo
    rm repo.zip
fi

cd repo

# Generate production configuration
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Generating production configuration..."
mkdir -p FileCategorization_Web/wwwroot
cat > FileCategorization_Web/wwwroot/appsettings.json << EOF
{
  "Uri": "$API_URL",
  "FileCategorizationApi": {
    "BaseUrl": "$API_URL"
  }
}
EOF

# Check available memory before build
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Checking system resources..."
FREE_MEM=$(free -m | awk 'NR==2{printf "%.0f", $7}')
echo "Available memory: ${FREE_MEM}MB"

if [ "$FREE_MEM" -lt 50 ]; then
    echo "❌ ERROR: Critically low memory detected (${FREE_MEM}MB)"
    echo "ARM32 Docker builds require at least 50MB available memory"
    echo "Please stop other services or reboot system to free memory"
    echo "Current memory usage:"
    free -h
    exit 1
elif [ "$FREE_MEM" -lt 200 ]; then
    echo "⚠️  WARNING: Low memory detected (${FREE_MEM}MB). Build may fail."
    echo "Attempting to free additional memory..."
    
    # Stop more services to free memory
    systemctl stop container-station 2>/dev/null || true
    sleep 5
    systemctl start container-station 2>/dev/null || true
    sleep 10
fi

# Build Docker image with memory optimizations
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Building ARM32 memory-optimized image..."
echo "Using dockerfile: Delivery/web.dockerfile"
echo "Build context: . (project root)"

# Memory optimization: containers will be managed manually

# Try build without memory limits first (ARM32 needs all available memory)
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Attempting build without memory constraints..."
if docker build \
    --platform linux/arm/v7 \
    -f Delivery/web.dockerfile \
    -t $IMAGE_NAME \
    . 2>/dev/null; then
    echo "✅ Build successful with platform specification"
else
    echo "⚠️  Platform build failed, trying without platform flag..."
    if docker build \
        -f Delivery/web.dockerfile \
        -t $IMAGE_NAME \
        . 2>/dev/null; then
        echo "✅ Build successful without platform specification"
    else
        echo "❌ Standard build failed, trying minimal resource build..."
        # Last resort: use legacy builder
        DOCKER_BUILDKIT=0 docker build \
            -f Delivery/web.dockerfile \
            -t $IMAGE_NAME \
            .
        
        if [ $? -ne 0 ]; then
            echo "❌ ERROR: All build attempts failed"
            echo "This usually indicates insufficient memory or corrupted Docker state"
            echo "Recommendations:"
            echo "1. Restart Docker daemon: systemctl restart docker"
            echo "2. Clear all Docker data: docker system prune -a -f"
            echo "3. Reboot system if memory is critically low"
            exit 1
        fi
    fi
fi

# Create volume directories
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Creating volume directories..."
mkdir -p /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/logs
mkdir -p /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/certs

# Start container with memory limits
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting container with ARM32 optimizations..."
docker run -d \
    --name $CONTAINER_NAME \
    --restart unless-stopped \
    --memory=256m \
    --memory-swap=256m \
    --cpus="0.5" \
    -p $PORT \
    -v /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/logs:/var/log/nginx \
    -v /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/certs:/etc/nginx/ssl \
    $IMAGE_NAME

# Wait and verify with multiple checks
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Waiting for container startup..."
sleep 15

# Check if container is running
if ! docker ps | grep -q $CONTAINER_NAME; then
    echo "❌ ERROR: Container failed to start"
    echo "Checking container logs..."
    docker logs $CONTAINER_NAME
    
    echo "Checking system resources..."
    free -h
    docker system df
    
    exit 1
fi

# Verify web service is responding
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Verifying web service..."
sleep 5

# Test with timeout to avoid hanging
if timeout 10 curl -f http://localhost:30229/ >/dev/null 2>&1; then
    echo "✅ SUCCESS: FileCategorization Web deployed successfully!"
    echo ""
    echo "📊 Deployment Information:"
    echo "   🌐 Web UI: http://YOUR_QNAP_IP:30229"
    echo "   📡 API URL: $API_URL"
    echo "   🐳 Container: $CONTAINER_NAME"
    echo "   🔧 Resource Limits: 256MB RAM, 0.5 CPU"
    echo ""
    echo "📁 Volume Mappings:"
    echo "   Nginx Logs: /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/logs"
    echo "   SSL Certs: /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/certs"
    echo ""
    echo "🔧 Management Commands:"
    echo "   docker logs -f $CONTAINER_NAME    # View live logs"
    echo "   docker restart $CONTAINER_NAME    # Restart container"
    echo "   docker stats $CONTAINER_NAME      # Resource usage"
else
    echo "⚠️  WARNING: Container started but web service not responding"
    echo "This may be normal for ARM32 - allow 1-2 minutes for full startup"
    echo "Check manually: http://YOUR_QNAP_IP:30229"
fi

# Resource summary
echo ""
echo "📊 Current Resource Usage:"
docker stats --no-stream $CONTAINER_NAME 2>/dev/null || true
free -h

# Cleanup
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Cleaning up temporary files..."
cd /
rm -rf $WORK_DIR

echo "============================================================"
echo "  ARM32 optimized deployment completed!"
echo "============================================================"