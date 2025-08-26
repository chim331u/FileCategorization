#!/bin/bash

#############################################################################
# Script di Test per il Deployment QNAP WEB
# Verifica che i parametri siano configurati correttamente
#############################################################################

# Colori per output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log() {
    echo -e "${BLUE}[TEST]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}

success() {
    echo -e "${GREEN}[OK]${NC} $1"
}

warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

echo "============================================================================="
echo "  Test Configurazione Deployment QNAP ARM32 - WEB"
echo "============================================================================="

# Set default configuration values (mirroring deploy_qnap_web.sh)
GITHUB_REPO="${GITHUB_REPO:-https://github.com/chim331u/FileCategorization.git}"
GIT_BRANCH="${GIT_BRANCH:-DeliveryNasArm32}"
CONTAINER_NAME="${CONTAINER_NAME:-filecat_web}"
HOST_PORT="${HOST_PORT:-80}"
HOST_HTTPS_PORT="${HOST_HTTPS_PORT:-443}"
DOCKER_IMAGE_NAME="${DOCKER_IMAGE_NAME:-filecat_web_image}"
DOCKER_IMAGE_TAG="${DOCKER_IMAGE_TAG:-latest}"
API_BASE_URL="${API_BASE_URL:-http://localhost:30219/}"
NGINX_LOG_VOLUME="${NGINX_LOG_VOLUME:-/share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/logs:/var/log/nginx}"
SSL_CERT_VOLUME="${SSL_CERT_VOLUME:-/share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/certs:/etc/nginx/ssl}"

echo ""
log "Testing configuration parameters..."

# Test repository URL
if [[ -n "$GITHUB_REPO" ]]; then
    success "GITHUB_REPO: $GITHUB_REPO"
else
    error "GITHUB_REPO is not set"
fi

# Test branch name
if [[ -n "$GIT_BRANCH" ]]; then
    success "GIT_BRANCH: $GIT_BRANCH"
    
    # Check for spaces in branch name
    if [[ "$GIT_BRANCH" =~ [[:space:]] ]]; then
        error "Branch name contains spaces: '$GIT_BRANCH'"
    else
        success "Branch name is valid (no spaces)"
    fi
else
    error "GIT_BRANCH is not set"
fi

# Test API base URL
if [[ -n "$API_BASE_URL" ]]; then
    if [[ "$API_BASE_URL" =~ ^https?:// ]]; then
        success "API_BASE_URL: $API_BASE_URL"
    else
        error "API_BASE_URL must be a valid HTTP/HTTPS URL: $API_BASE_URL"
    fi
else
    error "API_BASE_URL is not set"
fi

# Test Docker configuration
if [[ -n "$CONTAINER_NAME" ]]; then
    success "CONTAINER_NAME: $CONTAINER_NAME"
else
    error "CONTAINER_NAME is not set"
fi

if [[ -n "$HOST_PORT" ]]; then
    if [[ "$HOST_PORT" =~ ^[0-9]+$ ]] && [[ "$HOST_PORT" -ge 1 && "$HOST_PORT" -le 65535 ]]; then
        success "HOST_PORT: $HOST_PORT"
    else
        error "HOST_PORT must be a valid port number (1-65535): $HOST_PORT"
    fi
else
    error "HOST_PORT is not set"
fi

if [[ -n "$HOST_HTTPS_PORT" ]]; then
    if [[ "$HOST_HTTPS_PORT" =~ ^[0-9]+$ ]] && [[ "$HOST_HTTPS_PORT" -ge 1 && "$HOST_HTTPS_PORT" -le 65535 ]]; then
        success "HOST_HTTPS_PORT: $HOST_HTTPS_PORT"
    else
        error "HOST_HTTPS_PORT must be a valid port number (1-65535): $HOST_HTTPS_PORT"
    fi
else
    error "HOST_HTTPS_PORT is not set"
fi

# Test volume paths
echo ""
log "Testing volume configurations..."

if [[ -n "$NGINX_LOG_VOLUME" ]]; then
    NGINX_LOG_PATH=$(echo "$NGINX_LOG_VOLUME" | cut -d':' -f1)
    success "NGINX_LOG_VOLUME: $NGINX_LOG_VOLUME"
    log "Nginx log path: $NGINX_LOG_PATH"
else
    error "NGINX_LOG_VOLUME is not set"
fi

if [[ -n "$SSL_CERT_VOLUME" ]]; then
    SSL_CERT_PATH=$(echo "$SSL_CERT_VOLUME" | cut -d':' -f1)
    success "SSL_CERT_VOLUME: $SSL_CERT_VOLUME"
    log "SSL cert path: $SSL_CERT_PATH"
else
    error "SSL_CERT_VOLUME is not set"
fi

echo ""
log "Testing git clone command format..."

# Test del comando git clone che sarà eseguito
if [[ "$GIT_BRANCH" == "main" ]] || [[ "$GIT_BRANCH" == "master" ]]; then
    GIT_COMMAND="git clone \"$GITHUB_REPO\" \"/tmp/FileCategorization_Web\""
else
    GIT_COMMAND="git clone -b \"$GIT_BRANCH\" \"$GITHUB_REPO\" \"/tmp/FileCategorization_Web\""
fi

success "Git command to execute:"
echo "  $GIT_COMMAND"

echo ""
log "Testing Docker run command format..."

DOCKER_RUN_CMD="docker run \\
    --restart always \\
    --name \"$CONTAINER_NAME\" \\
    -d \\
    -p \"${HOST_PORT}:80\""

if [[ "$HOST_HTTPS_PORT" != "$HOST_PORT" ]]; then
    DOCKER_RUN_CMD="$DOCKER_RUN_CMD \\
    -p \"${HOST_HTTPS_PORT}:443\""
fi

DOCKER_RUN_CMD="$DOCKER_RUN_CMD \\
    -v \"$NGINX_LOG_VOLUME\" \\
    -v \"$SSL_CERT_VOLUME\" \\
    -e \"NGINX_WORKER_PROCESSES=1\" \\
    -e \"NGINX_WORKER_CONNECTIONS=1024\" \\
    -e \"ENABLE_GZIP=true\" \\
    -e \"GZIP_COMP_LEVEL=6\" \\
    --platform linux/arm/v7 \\
    \"${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}\""

success "Docker run command preview:"
echo "$DOCKER_RUN_CMD"

echo ""
log "Testing appsettings.json generation..."

APPSETTINGS_CONTENT="{
  \"FileCategorizationApi\": {
    \"BaseUrl\": \"$API_BASE_URL\",
    \"Timeout\": \"00:00:30\"
  },
  \"Uri\": \"$API_BASE_URL\"
}"

success "appsettings.json content preview:"
echo "$APPSETTINGS_CONTENT"

echo ""
log "Testing deployment verification..."

WEB_TEST_URL="http://localhost:${HOST_PORT}/"
success "Web URL to test: $WEB_TEST_URL"

if [[ "$HOST_HTTPS_PORT" != "$HOST_PORT" ]]; then
    HTTPS_TEST_URL="https://localhost:${HOST_HTTPS_PORT}/"
    success "HTTPS URL to test: $HTTPS_TEST_URL"
fi

echo ""
echo "============================================================================="
echo "  Configuration test completed!"
echo "============================================================================="
echo ""
echo "If all tests passed, you can now run:"
echo "  ./deploy_qnap_web.sh"
echo ""
echo "Or with custom parameters:"
echo "  ./deploy_qnap_web.sh --api-url \"$API_BASE_URL\" --port $HOST_PORT"
echo ""
echo "Make sure FileCategorization API is deployed first:"
echo "  ./deploy_qnap_api.sh"
echo ""