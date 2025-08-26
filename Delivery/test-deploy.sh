#!/bin/bash

#############################################################################
# Script di Test per il Deployment QNAP
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
echo "  Test Configurazione Deployment QNAP ARM32"
echo "============================================================================="

# Carica il file di configurazione
CONFIG_FILE="deploy_qnap_config.env"

if [[ -f "$CONFIG_FILE" ]]; then
    log "Loading configuration from: $CONFIG_FILE"
    source "$CONFIG_FILE"
    success "Configuration loaded"
else
    error "Configuration file not found: $CONFIG_FILE"
    exit 1
fi

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

# Test JWT secret length
if [[ -n "$JWT_SECRET" ]]; then
    if [[ ${#JWT_SECRET} -ge 32 ]]; then
        success "JWT_SECRET length: ${#JWT_SECRET} characters (OK)"
    else
        error "JWT_SECRET too short: ${#JWT_SECRET} characters (minimum: 32)"
    fi
else
    error "JWT_SECRET is not set"
fi

# Test DD credentials
if [[ -n "$DD_USERNAME" ]]; then
    success "DD_USERNAME: $DD_USERNAME"
else
    error "DD_USERNAME is not set"
fi

if [[ -n "$DD_PSW" ]]; then
    success "DD_PSW: [HIDDEN] (${#DD_PSW} characters)"
else
    error "DD_PSW is not set"
fi

# Test Docker configuration
if [[ -n "$CONTAINER_NAME" ]]; then
    success "CONTAINER_NAME: $CONTAINER_NAME"
else
    error "CONTAINER_NAME is not set"
fi

if [[ -n "$HOST_PORT" ]]; then
    success "HOST_PORT: $HOST_PORT"
else
    error "HOST_PORT is not set"
fi

# Test volume paths
echo ""
log "Testing volume configurations..."

if [[ -n "$DATA_VOLUME" ]]; then
    DATA_PATH=$(echo "$DATA_VOLUME" | cut -d':' -f1)
    success "DATA_VOLUME: $DATA_VOLUME"
    log "Data path: $DATA_PATH"
else
    error "DATA_VOLUME is not set"
fi

if [[ -n "$INCOMING_VOLUME" ]]; then
    INCOMING_PATH=$(echo "$INCOMING_VOLUME" | cut -d':' -f1)
    success "INCOMING_VOLUME: $INCOMING_VOLUME"
    log "Incoming path: $INCOMING_PATH"
else
    error "INCOMING_VOLUME is not set"
fi

if [[ -n "$SERIE_VOLUME" ]]; then
    SERIE_PATH=$(echo "$SERIE_VOLUME" | cut -d':' -f1)
    success "SERIE_VOLUME: $SERIE_VOLUME"
    log "Serie path: $SERIE_PATH"
else
    error "SERIE_VOLUME is not set"
fi

echo ""
log "Testing git clone command format..."

# Test del comando git clone che sarà eseguito
if [[ "$GIT_BRANCH" == "main" ]] || [[ "$GIT_BRANCH" == "master" ]]; then
    GIT_COMMAND="git clone \"$GITHUB_REPO\" \"$LOCAL_REPO_DIR\""
else
    GIT_COMMAND="git clone -b \"$GIT_BRANCH\" \"$GITHUB_REPO\" \"$LOCAL_REPO_DIR\""
fi

success "Git command to execute:"
echo "  $GIT_COMMAND"

echo ""
log "Testing Docker run command format..."

DOCKER_RUN_CMD="docker run \\
    --restart always \\
    --name \"$CONTAINER_NAME\" \\
    -d \\
    -p \"${HOST_PORT}:${CONTAINER_PORT}\" \\
    -v \"$DATA_VOLUME\" \\
    -v \"$INCOMING_VOLUME\" \\
    -v \"$SERIE_VOLUME\" \\
    -e \"JWT:Secret=$JWT_SECRET\" \\
    -e \"CRYPTO:MASTERKEY=$CRYPTO_MASTERKEY\" \\
    -e \"DD_USERNAME=$DD_USERNAME\" \\
    -e \"DD_PSW=$DD_PSW\" \\
    --platform linux/arm/v7 \\
    \"${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}\""

success "Docker run command preview:"
echo "$DOCKER_RUN_CMD"

echo ""
echo "============================================================================="
echo "  Configuration test completed!"
echo "============================================================================="
echo ""
echo "If all tests passed, you can now run:"
echo "  source $CONFIG_FILE && ./deploy_qnap_api.sh"
echo ""