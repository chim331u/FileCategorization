#!/bin/bash

# Simple QNAP Web Deployment - ARM32 Compatible
# Run this on your QNAP NAS to deploy the fixed web application

set -e

echo "=== FileCategorization WEB - ARM32 QNAP Deployment ==="
echo "Repository: https://github.com/chim331u/FileCategorization.git"
echo "Branch: DeliveryNasArm32"
echo "Fixed: wasm-tools issue for ARM32"
echo "========================================================="

# Configuration
REPO_URL="https://github.com/chim331u/FileCategorization.git"
BRANCH="DeliveryNasArm32"
CONTAINER_NAME="filecat_web"
IMAGE_NAME="filecat_web_image:latest"
PORT="30229:80"
API_URL="http://localhost:30219/"
WORK_DIR="/tmp/FileCategorization_Web"

# Clean any previous attempts
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Cleaning previous deployment..."
docker rm -f $CONTAINER_NAME 2>/dev/null || true
docker rmi $IMAGE_NAME 2>/dev/null || true
rm -rf $WORK_DIR

# Create working directory
mkdir -p $WORK_DIR
cd $WORK_DIR

# Download repository
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Downloading repository..."
if command -v git >/dev/null 2>&1; then
    git clone --depth 1 --branch $BRANCH $REPO_URL .
else
    ZIP_URL="https://github.com/chim331u/FileCategorization/archive/refs/heads/$BRANCH.zip"
    wget -O repo.zip "$ZIP_URL"
    unzip -q repo.zip
    mv FileCategorization-$BRANCH/* .
    rm repo.zip
fi

# Generate appsettings.json
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

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Configuration created: $API_URL"

# Build Docker image
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Building ARM32 optimized image..."
docker build -f Delivery/web.dockerfile -t $IMAGE_NAME .

# Create and start container
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting container..."
docker run -d \
    --name $CONTAINER_NAME \
    --restart unless-stopped \
    -p $PORT \
    $IMAGE_NAME

# Verify deployment
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Verifying deployment..."
sleep 5
if docker ps | grep -q $CONTAINER_NAME; then
    echo "✅ SUCCESS: Container $CONTAINER_NAME is running"
    echo "🌐 Web UI: http://YOUR_QNAP_IP:30229"
    echo "📊 API: $API_URL"
else
    echo "❌ FAILED: Container failed to start"
    docker logs $CONTAINER_NAME
    exit 1
fi

# Clean up
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Cleaning up..."
cd /
rm -rf $WORK_DIR

echo "=== Deployment Complete ==="