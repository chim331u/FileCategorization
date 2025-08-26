#!/bin/bash

#############################################################################
# FileCategorization API Deployment Script for QNAP ARM32 NAS
# Optimized for ARM32 architecture with 1GB RAM
# 
# This script performs:
# 1. Git clone from specified repository and branch
# 2. Docker build optimized for ARM32 
# 3. Docker run with configurable parameters and environment variables
#############################################################################

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}

success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

#############################################################################
# CONFIGURATION PARAMETERS
# These can be modified or set via environment variables
#############################################################################

# Git Repository Configuration
GITHUB_REPO="${GITHUB_REPO:-https://github.com/chim331u/FileCategorization.git}"
GIT_BRANCH="${GIT_BRANCH:-DeliveryNasArm32}"  # Default branch optimized for QNAP ARM32
LOCAL_REPO_DIR="${LOCAL_REPO_DIR:-/tmp/FileCategorization}"

# Docker Configuration  
DOCKER_IMAGE_NAME="${DOCKER_IMAGE_NAME:-filecat_api_image}"
DOCKER_IMAGE_TAG="${DOCKER_IMAGE_TAG:-latest}"
CONTAINER_NAME="${CONTAINER_NAME:-filecat_api}"

# Container Runtime Configuration
HOST_PORT="${HOST_PORT:-30219}"
CONTAINER_PORT="${CONTAINER_PORT:-8080}"

# Volume Mappings (QNAP specific paths)
DATA_VOLUME="${DATA_VOLUME:-/share/CACHEDEV2_DATA/Storage/Docker/file_categorization:/data}"
INCOMING_VOLUME="${INCOMING_VOLUME:-/share/Download/Incoming:/incoming}"
SERIE_VOLUME="${SERIE_VOLUME:-/share/Video/Serie:/serie}"

# Environment Variables for Application
JWT_SECRET="${JWT_SECRET:-your-32-characters-long-super-strong-jwt-secret-key}"
CRYPTO_MASTERKEY="${CRYPTO_MASTERKEY:-CrypToMasterKey}"
DD_USERNAME="${DD_USERNAME:-chim}"
DD_PSW="${DD_PSW:-aneurysm}"

# Docker Build Configuration
DOCKERFILE_PATH="${DOCKERFILE_PATH:-Delivery/api.dockerfile}"
BUILD_CONTEXT="${BUILD_CONTEXT:-.}"

#############################################################################
# PARAMETER VALIDATION
#############################################################################

validate_parameters() {
    log "Validating deployment parameters..."
    
    # Validate repository URL
    if [[ -z "$GITHUB_REPO" ]]; then
        error "GITHUB_REPO is required"
        exit 1
    fi
    
    if [[ ! "$GITHUB_REPO" =~ ^https?://github\.com/.+\.git$ ]]; then
        warning "GITHUB_REPO format may be invalid: $GITHUB_REPO"
        warning "Expected format: https://github.com/chim331u/repository.git"
    fi
    
    # Validate branch name
    if [[ -z "$GIT_BRANCH" ]]; then
        error "GIT_BRANCH is required"
        exit 1
    fi
    
    # Check for potentially problematic characters in branch name
    if [[ "$GIT_BRANCH" =~ [[:space:]] ]]; then
        error "GIT_BRANCH contains spaces: '$GIT_BRANCH'"
        exit 1
    fi
    
    # Validate JWT secret
    if [[ ${#JWT_SECRET} -lt 32 ]]; then
        error "JWT_SECRET must be at least 32 characters long (current: ${#JWT_SECRET})"
        exit 1
    fi
    
    # Validate DownloadDaemon credentials
    if [[ -z "$DD_USERNAME" || -z "$DD_PSW" ]]; then
        error "DD_USERNAME and DD_PSW are required"
        exit 1
    fi
    
    log "Repository: $GITHUB_REPO"
    log "Branch: $GIT_BRANCH"
    log "Container: $CONTAINER_NAME"
    log "Port: $HOST_PORT:$CONTAINER_PORT"
    
    success "Parameter validation completed"
}

#############################################################################
# QNAP NAS OPTIMIZATION CHECKS
#############################################################################

check_qnap_environment() {
    log "Checking QNAP NAS environment..."
    
    # Check if running on ARM32 architecture
    ARCH=$(uname -m)
    if [[ "$ARCH" != "armv7l" ]]; then
        warning "Not running on ARM32 architecture (detected: $ARCH)"
    else
        success "ARM32 architecture detected"
    fi
    
    # Check available memory
    TOTAL_MEM=$(free -m | awk 'NR==2{printf "%.0f", $2}')
    if [[ $TOTAL_MEM -lt 1000 ]]; then
        warning "Available memory is low: ${TOTAL_MEM}MB"
    fi
    
    # Check download tools
    if command -v git >/dev/null 2>&1; then
        success "Git available - will use git clone"
    elif command -v wget >/dev/null 2>&1; then
        success "wget available - will use ZIP download"
    elif command -v curl >/dev/null 2>&1; then
        success "curl available - will use ZIP download"
    else
        error "Neither git, wget, nor curl found. Cannot download repository."
        exit 1
    fi
    
    # Check extraction tools (needed for ZIP method)
    if ! command -v git >/dev/null 2>&1; then
        if ! command -v unzip >/dev/null 2>&1; then
            error "unzip command not found. Required for ZIP extraction."
            log "Please install unzip: opkg install unzip"
            exit 1
        else
            success "unzip available for ZIP extraction"
        fi
    fi
    
    # Check Docker
    if ! command -v docker >/dev/null 2>&1; then
        error "Docker not found. Please enable Container Station."
        exit 1
    else
        success "Docker available"
    fi
    
    # Check if required directories exist
    for dir in "/share/CACHEDEV2_DATA" "/share/Download" "/share/Video"; do
        if [[ ! -d "$dir" ]]; then
            warning "Directory $dir does not exist"
        fi
    done
}

#############################################################################
# DOCKER MANAGEMENT FUNCTIONS
#############################################################################

cleanup_existing_container() {
    log "Checking for existing container: $CONTAINER_NAME"
    
    if docker ps -a --format "table {{.Names}}" | grep -q "^${CONTAINER_NAME}$"; then
        log "Stopping and removing existing container: $CONTAINER_NAME"
        docker stop "$CONTAINER_NAME" 2>/dev/null || true
        docker rm "$CONTAINER_NAME" 2>/dev/null || true
        success "Existing container removed"
    else
        log "No existing container found"
    fi
}

cleanup_existing_image() {
    local image_full_name="${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}"
    
    log "Checking for existing image: $image_full_name"
    
    if docker images --format "table {{.Repository}}:{{.Tag}}" | grep -q "^${image_full_name}$"; then
        log "Removing existing image: $image_full_name"
        docker rmi "$image_full_name" 2>/dev/null || true
        success "Existing image removed"
    else
        log "No existing image found"
    fi
}

#############################################################################
# GIT OPERATIONS
#############################################################################

download_repository_zip() {
    log "Downloading repository as ZIP archive..."
    
    # Convert GitHub repo URL to ZIP download URL
    # From: https://github.com/user/repo.git
    # To:   https://github.com/user/repo/archive/refs/heads/branch.zip
    local repo_base=$(echo "$GITHUB_REPO" | sed 's/\.git$//')
    local zip_url="${repo_base}/archive/refs/heads/${GIT_BRANCH}.zip"
    local zip_file="/tmp/repo_${GIT_BRANCH}.zip"
    
    log "ZIP URL: $zip_url"
    log "ZIP file: $zip_file"
    
    # Download ZIP file
    if command -v wget >/dev/null 2>&1; then
        log "Using wget to download..."
        wget -O "$zip_file" "$zip_url"
    elif command -v curl >/dev/null 2>&1; then
        log "Using curl to download..."
        curl -L -o "$zip_file" "$zip_url"
    else
        error "Neither wget nor curl found. Cannot download repository."
        exit 1
    fi
    
    # Verify download
    if [[ ! -f "$zip_file" ]]; then
        error "Failed to download repository ZIP"
        exit 1
    fi
    
    # Extract ZIP file
    log "Extracting ZIP archive..."
    if command -v unzip >/dev/null 2>&1; then
        unzip -q "$zip_file" -d "/tmp/"
        
        # GitHub ZIP extracts to folder named "repo-branch"
        local repo_name=$(basename "$GITHUB_REPO" .git)
        local extracted_dir="/tmp/${repo_name}-${GIT_BRANCH}"
        
        if [[ -d "$extracted_dir" ]]; then
            mv "$extracted_dir" "$LOCAL_REPO_DIR"
            success "Repository extracted to: $LOCAL_REPO_DIR"
        else
            error "Extracted directory not found: $extracted_dir"
            exit 1
        fi
    else
        error "unzip command not found. Cannot extract repository."
        exit 1
    fi
    
    # Cleanup
    rm -f "$zip_file"
}

clone_repository() {
    log "Downloading repository: $GITHUB_REPO (branch: $GIT_BRANCH)"
    
    # Remove existing local repository if exists
    if [[ -d "$LOCAL_REPO_DIR" ]]; then
        log "Removing existing local repository: $LOCAL_REPO_DIR"
        rm -rf "$LOCAL_REPO_DIR"
    fi
    
    # Check if git is available
    if command -v git >/dev/null 2>&1; then
        log "Git found - using git clone"
        # Clone repository with specific branch
        if [[ "$GIT_BRANCH" == "main" ]] || [[ "$GIT_BRANCH" == "master" ]]; then
            log "Cloning default branch: git clone \"$GITHUB_REPO\" \"$LOCAL_REPO_DIR\""
            git clone "$GITHUB_REPO" "$LOCAL_REPO_DIR"
        else
            log "Cloning specific branch: git clone -b \"$GIT_BRANCH\" \"$GITHUB_REPO\" \"$LOCAL_REPO_DIR\""
            git clone -b "$GIT_BRANCH" "$GITHUB_REPO" "$LOCAL_REPO_DIR"
        fi
    else
        log "Git not found - using wget/curl to download ZIP archive"
        download_repository_zip
    fi
    
    # Verify download success and find Dockerfile
    if [[ ! -d "$LOCAL_REPO_DIR" ]]; then
        error "Repository download failed - directory not found: $LOCAL_REPO_DIR"
        exit 1
    fi
    
    cd "$LOCAL_REPO_DIR"
    
    # Search for Dockerfile in multiple possible locations (order matters - most specific first)
    POSSIBLE_DOCKERFILES=(
        "$DOCKERFILE_PATH"
        "Delivery/api.dockerfile"
        "Delivery/api-simple.dockerfile"
        "Delivery/api-minimal.dockerfile"
        "api.dockerfile"
        "api-simple.dockerfile"
        "api-minimal.dockerfile"
        "Dockerfile.api"
        "Dockerfile"
    )
    
    FOUND_DOCKERFILE=""
    for dockerfile in "${POSSIBLE_DOCKERFILES[@]}"; do
        if [[ -f "$dockerfile" ]]; then
            FOUND_DOCKERFILE="$dockerfile"
            log "Found Dockerfile at: $dockerfile"
            break
        fi
    done
    
    # If not found, do a recursive search
    if [[ -z "$FOUND_DOCKERFILE" ]]; then
        log "Dockerfile not found in expected locations, searching recursively..."
        
        # Search for api.dockerfile anywhere in the repository
        RECURSIVE_SEARCH=$(find . -name "api.dockerfile" -type f | head -1)
        if [[ -n "$RECURSIVE_SEARCH" ]]; then
            FOUND_DOCKERFILE="$RECURSIVE_SEARCH"
            log "Found Dockerfile recursively at: $RECURSIVE_SEARCH"
        else
            # Search for any dockerfile
            RECURSIVE_SEARCH=$(find . -name "*dockerfile*" -type f | head -1)
            if [[ -n "$RECURSIVE_SEARCH" ]]; then
                FOUND_DOCKERFILE="$RECURSIVE_SEARCH"
                log "Found alternative Dockerfile at: $RECURSIVE_SEARCH"
            fi
        fi
    fi
    
    if [[ -z "$FOUND_DOCKERFILE" ]]; then
        error "Dockerfile not found. Searched locations:"
        for dockerfile in "${POSSIBLE_DOCKERFILES[@]}"; do
            error "  - $dockerfile"
        done
        log "Current directory: $(pwd)"
        log "Repository directory structure:"
        ls -la
        log "All files in repository:"
        find . -type f | head -20
        log "Searching for dockerfile recursively:"
        find . -name "*dockerfile*" -o -name "Dockerfile*"
        log "Searching for Delivery directory:"
        find . -type d -name "Delivery" -o -name "delivery"
        exit 1
    fi
    
    # Update DOCKERFILE_PATH to the found location
    DOCKERFILE_PATH="$FOUND_DOCKERFILE"
    success "Repository downloaded successfully"
    
    # Show current info
    if command -v git >/dev/null 2>&1 && [[ -d ".git" ]]; then
        CURRENT_COMMIT=$(git rev-parse --short HEAD 2>/dev/null)
        CURRENT_BRANCH=$(git branch --show-current 2>/dev/null)
        log "Current commit: $CURRENT_COMMIT on branch: $CURRENT_BRANCH"
    else
        log "Downloaded as ZIP archive (no git history)"
    fi
}

#############################################################################
# DOCKER BUILD OPERATIONS
#############################################################################

build_docker_image() {
    log "Building Docker image for ARM32 architecture..."
    
    cd "$LOCAL_REPO_DIR"
    
    # Verify Dockerfile exists (should already be verified, but double-check)
    if [[ ! -f "$DOCKERFILE_PATH" ]]; then
        error "Dockerfile not found at: $DOCKERFILE_PATH"
        log "Current directory contents:"
        ls -la
        exit 1
    fi
    
    log "Using Dockerfile: $DOCKERFILE_PATH"
    
    local image_full_name="${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}"
    
    # Build Docker image with ARM32 platform specification
    log "Building image: $image_full_name"
    log "Dockerfile: $DOCKERFILE_PATH"
    log "Build context: $BUILD_CONTEXT"
    
    # Try to build with platform specification first
    log "Attempting build with --platform linux/arm/v7"
    if ! docker build \
        --platform linux/arm/v7 \
        -f "$DOCKERFILE_PATH" \
        -t "$image_full_name" \
        "$BUILD_CONTEXT"; then
        
        warning "Build with --platform failed, trying without platform specification"
        
        # Fallback: build without platform specification
        if ! docker build \
            -f "$DOCKERFILE_PATH" \
            -t "$image_full_name" \
            "$BUILD_CONTEXT"; then
            
            error "Docker build failed with both platform and no-platform approaches"
            
            # Try fallback Dockerfiles in order of preference
            FALLBACK_DOCKERFILES=(
                "Delivery/api-simple.dockerfile"
                "Delivery/api-minimal.dockerfile"
            )
            
            FALLBACK_SUCCESS=false
            for fallback_dockerfile in "${FALLBACK_DOCKERFILES[@]}"; do
                if [[ -f "$fallback_dockerfile" && "$DOCKERFILE_PATH" != "$fallback_dockerfile" ]]; then
                    warning "Trying fallback with $fallback_dockerfile"
                    
                    if docker build \
                        -f "$fallback_dockerfile" \
                        -t "$image_full_name" \
                        "$BUILD_CONTEXT"; then
                        
                        DOCKERFILE_PATH="$fallback_dockerfile"
                        FALLBACK_SUCCESS=true
                        success "Fallback build successful with $fallback_dockerfile"
                        break
                    else
                        warning "Fallback build failed with $fallback_dockerfile"
                    fi
                fi
            done
            
            if [[ "$FALLBACK_SUCCESS" != "true" ]]; then
                error "All Dockerfile builds failed"
                exit 1
            fi
        fi
    fi
    
    # Verify image was created
    if ! docker images --format "table {{.Repository}}:{{.Tag}}" | grep -q "^${image_full_name}$"; then
        error "Docker image build failed"
        exit 1
    fi
    
    success "Docker image built successfully: $image_full_name"
    
    # Show image size for ARM32 optimization verification
    IMAGE_SIZE=$(docker images --format "table {{.Size}}" "$image_full_name" | tail -n +2)
    log "Image size: $IMAGE_SIZE"
}

#############################################################################
# DOCKER RUN OPERATIONS
#############################################################################

create_data_directories() {
    log "Creating required data directories..."
    
    # Extract host paths from volume mappings
    local data_host_path=$(echo "$DATA_VOLUME" | cut -d':' -f1)
    local incoming_host_path=$(echo "$INCOMING_VOLUME" | cut -d':' -f1)
    local serie_host_path=$(echo "$SERIE_VOLUME" | cut -d':' -f1)
    
    for dir in "$data_host_path" "$incoming_host_path" "$serie_host_path"; do
        if [[ ! -d "$dir" ]]; then
            log "Creating directory: $dir"
            mkdir -p "$dir" || warning "Failed to create directory: $dir"
        else
            log "Directory already exists: $dir"
        fi
    done
}

run_docker_container() {
    log "Starting Docker container: $CONTAINER_NAME"
    
    local image_full_name="${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}"
    
    # Create required directories
    create_data_directories
    
    # Run Docker container with all specified parameters
    docker run \
        --restart always \
        --name "$CONTAINER_NAME" \
        -d \
        -p "${HOST_PORT}:${CONTAINER_PORT}" \
        -v "$DATA_VOLUME" \
        -v "$INCOMING_VOLUME" \
        -v "$SERIE_VOLUME" \
        -e "JWT:Secret=$JWT_SECRET" \
        -e "CRYPTO:MASTERKEY=$CRYPTO_MASTERKEY" \
        -e "DD_USERNAME=$DD_USERNAME" \
        -e "DD_PSW=$DD_PSW" \
        --platform linux/arm/v7 \
        "$image_full_name"
    
    # Verify container is running
    sleep 5
    if ! docker ps --format "table {{.Names}}" | grep -q "^${CONTAINER_NAME}$"; then
        error "Container failed to start"
        log "Container logs:"
        docker logs "$CONTAINER_NAME"
        exit 1
    fi
    
    success "Container started successfully: $CONTAINER_NAME"
    
    # Show container status and resource usage
    log "Container status:"
    docker ps --filter "name=$CONTAINER_NAME" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    
    # Show container resource usage (ARM32 optimization check)
    log "Container resource usage:"
    docker stats "$CONTAINER_NAME" --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}"
}

#############################################################################
# DEPLOYMENT VERIFICATION
#############################################################################

verify_deployment() {
    log "Verifying deployment..."
    
    # Wait for application to start
    sleep 10
    
    # Check if container is still running
    if ! docker ps --format "table {{.Names}}" | grep -q "^${CONTAINER_NAME}$"; then
        error "Container is not running"
        return 1
    fi
    
    # Try to connect to the API endpoint
    local api_url="http://localhost:${HOST_PORT}/health"
    log "Testing API endpoint: $api_url"
    
    if command -v curl >/dev/null 2>&1; then
        if curl -f -s "$api_url" >/dev/null; then
            success "API endpoint is responding"
        else
            warning "API endpoint is not responding yet (this may be normal during startup)"
        fi
    else
        warning "curl not available for endpoint testing"
    fi
    
    # Show recent logs
    log "Recent container logs:"
    docker logs --tail 20 "$CONTAINER_NAME"
}

#############################################################################
# CLEANUP FUNCTIONS
#############################################################################

cleanup_temp_files() {
    if [[ -d "$LOCAL_REPO_DIR" ]]; then
        log "Cleaning up temporary repository: $LOCAL_REPO_DIR"
        rm -rf "$LOCAL_REPO_DIR"
    fi
}

#############################################################################
# MAIN DEPLOYMENT PROCESS
#############################################################################

print_banner() {
    echo "============================================================================="
    echo "  FileCategorization API - QNAP ARM32 NAS Deployment Script"
    echo "============================================================================="
    echo "Repository: $GITHUB_REPO"
    echo "Branch: $GIT_BRANCH"
    echo "Image: ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}"
    echo "Container: $CONTAINER_NAME"
    echo "Port: $HOST_PORT:$CONTAINER_PORT"
    echo "============================================================================="
}

print_summary() {
    echo ""
    echo "============================================================================="
    echo "  DEPLOYMENT COMPLETED SUCCESSFULLY"
    echo "============================================================================="
    echo "Container Name: $CONTAINER_NAME"
    echo "Image: ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}"
    echo "API URL: http://localhost:${HOST_PORT}"
    echo "Hangfire Dashboard: http://localhost:${HOST_PORT}/hangfire"
    echo ""
    echo "Useful commands:"
    echo "  docker logs $CONTAINER_NAME              # View container logs"
    echo "  docker restart $CONTAINER_NAME           # Restart container"
    echo "  docker stop $CONTAINER_NAME              # Stop container"
    echo "  docker stats $CONTAINER_NAME             # View resource usage"
    echo "============================================================================="
}

show_help() {
    cat << EOF
FileCategorization API Deployment Script for QNAP ARM32 NAS

USAGE:
    $0 [OPTIONS]

OPTIONS:
    -h, --help              Show this help message
    -r, --repo URL          Git repository URL (default: set GITHUB_REPO env var)
    -b, --branch NAME       Git branch name (default: main)
    -p, --port PORT         Host port for API (default: 30219)
    -n, --name NAME         Container name (default: filecat_api)
    -i, --image NAME        Docker image name (default: filecat_api_image)
    --jwt-secret SECRET     JWT secret key (32+ characters)
    --dd-username USER      DownloadDaemon username
    --dd-password PASS      DownloadDaemon password

ENVIRONMENT VARIABLES:
    GITHUB_REPO            Git repository URL
    GIT_BRANCH             Git branch to clone
    HOST_PORT              Host port for API
    CONTAINER_NAME         Docker container name
    DOCKER_IMAGE_NAME      Docker image name
    JWT_SECRET             JWT secret key
    DD_USERNAME            DownloadDaemon username  
    DD_PSW                 DownloadDaemon password

EXAMPLE:
    # Using command line parameters
    $0 -r https://github.com/user/FileCategorization.git -b main -p 30219

    # Using environment variables
    export GITHUB_REPO="https://github.com/user/FileCategorization.git"
    export JWT_SECRET="your-32-characters-long-super-strong-jwt-secret-key"
    export DD_USERNAME="myUsername"
    export DD_PSW="myPassword"
    $0

EOF
}

#############################################################################
# COMMAND LINE ARGUMENT PARSING
#############################################################################

parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_help
                exit 0
                ;;
            -r|--repo)
                GITHUB_REPO="$2"
                shift 2
                ;;
            -b|--branch)
                GIT_BRANCH="$2"
                shift 2
                ;;
            -p|--port)
                HOST_PORT="$2"
                shift 2
                ;;
            -n|--name)
                CONTAINER_NAME="$2"
                shift 2
                ;;
            -i|--image)
                DOCKER_IMAGE_NAME="$2"
                shift 2
                ;;
            --jwt-secret)
                JWT_SECRET="$2"
                shift 2
                ;;
            --dd-username)
                DD_USERNAME="$2"
                shift 2
                ;;
            --dd-password)
                DD_PSW="$2"
                shift 2
                ;;
            *)
                error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

#############################################################################
# MAIN EXECUTION
#############################################################################

main() {
    parse_arguments "$@"
    print_banner
    
    # Pre-deployment checks
    validate_parameters
    check_qnap_environment
    
    # Deployment process
    cleanup_existing_container
    cleanup_existing_image
    clone_repository
    build_docker_image
    run_docker_container
    verify_deployment
    
    # Cleanup
    cleanup_temp_files
    
    print_summary
    success "FileCategorization API deployed successfully on QNAP ARM32 NAS!"
}

# Trap to cleanup on script exit
trap cleanup_temp_files EXIT

# Execute main function with all arguments
main "$@"