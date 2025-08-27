#!/bin/bash

# QNAP ARM32 Web Deployment - All Issues Fixed
# This script addresses:
# 1. wasm-tools compatibility issue for ARM32
# 2. Proper build context for FileCategorization_Shared
# 3. Simplified dependency resolution

set -e

echo "============================================================"
echo "  FileCategorization WEB - QNAP ARM32 Fixed Deployment"
echo "============================================================"
echo "Repository: https://github.com/chim331u/FileCategorization.git"
echo "Branch: DeliveryNasArm32"
echo "Fixes: ARM32 wasm-tools, build context, dependencies"
echo "============================================================"

# Configuration
REPO_URL="https://github.com/chim331u/FileCategorization.git"
BRANCH="DeliveryNasArm32"
CONTAINER_NAME="filecat_web"
IMAGE_NAME="filecat_web_image:latest"
PORT="30229:80"
API_URL="http://localhost:30219/"
WORK_DIR="/tmp/FileCategorization_Fixed"

# Clean previous attempts
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Cleaning previous deployment..."
docker rm -f $CONTAINER_NAME 2>/dev/null || true
docker rmi $IMAGE_NAME 2>/dev/null || true
rm -rf $WORK_DIR

# Create working directory and download
mkdir -p $WORK_DIR
cd $WORK_DIR

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Downloading repository..."
if command -v git >/dev/null 2>&1; then
    git clone --depth 1 --branch $BRANCH $REPO_URL repo
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

# Build Docker image with proper context
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Building ARM32 optimized image..."
echo "Using dockerfile: Delivery/web.dockerfile"
echo "Build context: . (project root)"

docker build \
    --platform linux/arm/v7 \
    -f Delivery/web.dockerfile \
    -t $IMAGE_NAME \
    .

if [ $? -ne 0 ]; then
    echo "❌ ERROR: Docker build failed"
    echo "Attempting build without platform flag..."
    docker build \
        -f Delivery/web.dockerfile \
        -t $IMAGE_NAME \
        .
    
    if [ $? -ne 0 ]; then
        echo "❌ ERROR: Docker build failed completely"
        exit 1
    fi
fi

# Create volume directories
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Creating volume directories..."
mkdir -p /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/logs
mkdir -p /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/certs

# Start container
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting container..."
docker run -d \
    --name $CONTAINER_NAME \
    --restart unless-stopped \
    -p $PORT \
    -v /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/logs:/var/log/nginx \
    -v /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/certs:/etc/nginx/ssl \
    $IMAGE_NAME

# Wait and verify
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Waiting for container startup..."
sleep 10

if docker ps | grep -q $CONTAINER_NAME; then
    echo "✅ SUCCESS: FileCategorization Web deployed successfully!"
    echo ""
    echo "📊 Deployment Information:"
    echo "   🌐 Web UI: http://YOUR_QNAP_IP:30229"
    echo "   📡 API URL: $API_URL"
    echo "   🐳 Container: $CONTAINER_NAME"
    echo "   📝 Logs: docker logs $CONTAINER_NAME"
    echo ""
    echo "📁 Volume Mappings:"
    echo "   Nginx Logs: /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/logs"
    echo "   SSL Certs: /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/certs"
    echo ""
    echo "🔧 Management Commands:"
    echo "   docker logs -f $CONTAINER_NAME    # View live logs"
    echo "   docker restart $CONTAINER_NAME    # Restart container"
    echo "   docker rm -f $CONTAINER_NAME      # Remove container"
else
    echo "❌ ERROR: Container failed to start"
    echo "Checking logs..."
    docker logs $CONTAINER_NAME
    exit 1
fi

# Cleanup
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Cleaning up temporary files..."
cd /
rm -rf $WORK_DIR

echo "============================================================"
echo "  Deployment completed successfully!"
echo "============================================================"