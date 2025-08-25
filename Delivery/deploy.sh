#!/bin/bash

# 🚀 FileCategorization Automated Deployment Script for QNAP ARM32 NAS
# This script automates the complete deployment process:
# 1. Clone/update from GitHub
# 2. Build optimized ARM32 Docker images
# 3. Set up persistent storage
# 4. Deploy with Docker Compose
# 5. Configure system services

set -e  # Exit on any error
set -u  # Exit on undefined variables

# Configuration
readonly SCRIPT_NAME="FileCategorization Deploy"
readonly SCRIPT_VERSION="1.0.0"
readonly GITHUB_REPO="https://github.com/your-username/FileCategorization.git"
readonly PROJECT_NAME="filecategorization"
readonly INSTALL_DIR="/share/Container/${PROJECT_NAME}"
readonly DATA_DIR="${INSTALL_DIR}/data"
readonly LOG_DIR="${INSTALL_DIR}/logs"
readonly CONFIG_DIR="${INSTALL_DIR}/config"
readonly CERT_DIR="${INSTALL_DIR}/certs"
readonly BACKUP_DIR="${INSTALL_DIR}/backups"

# Colors for output
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly NC='\033[0m' # No Color

# Logging function
log() {
    local level="$1"
    shift
    local message="$*"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    
    case "$level" in
        "INFO")  echo -e "${GREEN}[INFO]${NC}  ${timestamp} - $message" ;;
        "WARN")  echo -e "${YELLOW}[WARN]${NC}  ${timestamp} - $message" ;;
        "ERROR") echo -e "${RED}[ERROR]${NC} ${timestamp} - $message" ;;
        "DEBUG") echo -e "${BLUE}[DEBUG]${NC} ${timestamp} - $message" ;;
        *)       echo -e "${timestamp} - $message" ;;
    esac
}

# Error handler
error_exit() {
    log "ERROR" "$1"
    log "ERROR" "Deployment failed. Check logs above for details."
    exit 1
}

# Check prerequisites
check_prerequisites() {
    log "INFO" "Checking system prerequisites..."
    
    # Check if running on ARM architecture
    local arch=$(uname -m)
    if [[ ! "$arch" =~ ^(armv7l|aarch64|arm)$ ]]; then
        log "WARN" "Warning: Not running on ARM architecture ($arch). Continuing anyway..."
    fi
    
    # Check required commands
    local required_commands=("docker" "git" "curl" "jq")
    for cmd in "${required_commands[@]}"; do
        if ! command -v "$cmd" &> /dev/null; then
            error_exit "Required command '$cmd' not found. Please install it first."
        fi
    done
    
    # Check Docker service
    if ! docker ps &> /dev/null; then
        error_exit "Docker is not running or not accessible. Please start Docker service."
    fi
    
    # Check available disk space (at least 2GB)
    local available_space=$(df /share 2>/dev/null | tail -1 | awk '{print $4}')
    if [ "$available_space" -lt 2097152 ]; then  # 2GB in KB
        log "WARN" "Warning: Less than 2GB available space. Deployment may fail."
    fi
    
    log "INFO" "Prerequisites check completed ✓"
}

# Create directory structure
setup_directories() {
    log "INFO" "Setting up directory structure..."
    
    local directories=(
        "$INSTALL_DIR"
        "$DATA_DIR"
        "$LOG_DIR"
        "$LOG_DIR/nginx" 
        "$CONFIG_DIR"
        "$CERT_DIR"
        "$BACKUP_DIR"
        "$DATA_DIR/MlModels"
    )
    
    for dir in "${directories[@]}"; do
        if [ ! -d "$dir" ]; then
            mkdir -p "$dir" || error_exit "Failed to create directory: $dir"
            log "DEBUG" "Created directory: $dir"
        fi
    done
    
    # Set proper permissions
    chmod -R 755 "$INSTALL_DIR"
    chown -R admin:administrators "$INSTALL_DIR" 2>/dev/null || true
    
    log "INFO" "Directory structure created ✓"
}

# Clone or update repository
clone_or_update_repo() {
    log "INFO" "Cloning/updating repository from GitHub..."
    
    local repo_dir="${INSTALL_DIR}/source"
    
    if [ -d "$repo_dir" ]; then
        log "INFO" "Repository exists, updating..."
        cd "$repo_dir"
        git fetch origin
        git reset --hard origin/main || error_exit "Failed to update repository"
        log "INFO" "Repository updated to latest version ✓"
    else
        log "INFO" "Cloning repository..."
        git clone "$GITHUB_REPO" "$repo_dir" || error_exit "Failed to clone repository"
        log "INFO" "Repository cloned successfully ✓"
    fi
    
    cd "$repo_dir"
    local latest_commit=$(git rev-parse --short HEAD)
    log "INFO" "Using commit: $latest_commit"
}

# Generate production configuration
generate_production_config() {
    log "INFO" "Generating production configuration..."
    
    local env_file="${INSTALL_DIR}/.env"
    local jwt_secret=$(openssl rand -base64 32 2>/dev/null || echo "change-this-jwt-secret-$(date +%s)")
    local nas_ip=$(hostname -I | awk '{print $1}' 2>/dev/null || echo "localhost")
    
    cat > "$env_file" << EOF
# FileCategorization Production Configuration
# Generated on: $(date)

# Security
JWT_SECRET=${jwt_secret}

# Network Configuration
NGINX_HOST=${nas_ip}
COMPOSE_PROJECT_NAME=${PROJECT_NAME}

# Paths
DATA_PATH=${DATA_DIR}
LOG_PATH=${LOG_DIR}
CONFIG_PATH=${CONFIG_DIR}

# Performance (ARM32 Optimized)
DOTNET_GCServer=0
DOTNET_GCConcurrent=1
DOTNET_GCRetainVM=25

# Timezone
TZ=Europe/Rome
EOF
    
    chmod 600 "$env_file"
    log "INFO" "Production configuration generated ✓"
    log "DEBUG" "NAS IP detected as: $nas_ip"
}

# Build Docker images
build_docker_images() {
    log "INFO" "Building Docker images for ARM32..."
    
    local repo_dir="${INSTALL_DIR}/source"
    cd "$repo_dir"
    
    # Build API image
    log "INFO" "Building API image..."
    docker build -f Delivery/api.dockerfile -t filecategorization-api:latest . || error_exit "Failed to build API image"
    
    # Build Web image  
    log "INFO" "Building Web image..."
    docker build -f Delivery/web.dockerfile -t filecategorization-web:latest . || error_exit "Failed to build Web image"
    
    log "INFO" "Docker images built successfully ✓"
    
    # Show image sizes
    log "DEBUG" "Docker images:"
    docker images | grep filecategorization
}

# Deploy with Docker Compose
deploy_containers() {
    log "INFO" "Deploying containers with Docker Compose..."
    
    local repo_dir="${INSTALL_DIR}/source"
    local compose_file="${repo_dir}/Delivery/docker-compose.yml"
    
    cd "$repo_dir/Delivery"
    
    # Stop existing containers
    docker-compose down 2>/dev/null || true
    
    # Deploy new containers
    docker-compose --env-file "${INSTALL_DIR}/.env" up -d || error_exit "Failed to deploy containers"
    
    log "INFO" "Containers deployed successfully ✓"
    
    # Wait for services to be ready
    log "INFO" "Waiting for services to start..."
    sleep 30
    
    # Check service health
    local api_health=$(curl -s -f http://localhost:5089/health 2>/dev/null && echo "OK" || echo "FAIL")
    local web_health=$(curl -s -f http://localhost/ 2>/dev/null && echo "OK" || echo "FAIL")
    
    log "INFO" "Service health check:"
    log "INFO" "  API Service: $api_health"
    log "INFO" "  Web Service: $web_health"
}

# Create system service (optional)
create_system_service() {
    log "INFO" "Creating system startup service..."
    
    local service_file="/etc/systemd/system/filecategorization.service"
    local repo_dir="${INSTALL_DIR}/source"
    
    # Only create service if systemd is available
    if command -v systemctl &> /dev/null; then
        cat > "$service_file" << EOF
[Unit]
Description=FileCategorization Docker Compose Service
After=docker.service
Requires=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=${repo_dir}/Delivery
ExecStart=/usr/bin/docker-compose --env-file ${INSTALL_DIR}/.env up -d
ExecStop=/usr/bin/docker-compose down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF
        
        systemctl daemon-reload
        systemctl enable filecategorization.service
        log "INFO" "System service created and enabled ✓"
    else
        log "WARN" "Systemd not available, skipping service creation"
    fi
}

# Create backup script
create_backup_script() {
    log "INFO" "Creating backup script..."
    
    local backup_script="${INSTALL_DIR}/backup.sh"
    
    cat > "$backup_script" << 'EOF'
#!/bin/bash
# FileCategorization Backup Script

BACKUP_DIR="/share/Container/filecategorization/backups"
DATA_DIR="/share/Container/filecategorization/data"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/filecategorization_backup_$TIMESTAMP.tar.gz"

echo "Starting backup at $(date)"

# Create backup
tar -czf "$BACKUP_FILE" -C "$DATA_DIR" . 2>/dev/null

if [ $? -eq 0 ]; then
    echo "Backup completed: $BACKUP_FILE"
    
    # Keep only last 7 backups
    cd "$BACKUP_DIR"
    ls -t filecategorization_backup_*.tar.gz | tail -n +8 | xargs -r rm
    
    echo "Old backups cleaned up"
else
    echo "Backup failed"
    exit 1
fi
EOF
    
    chmod +x "$backup_script"
    log "INFO" "Backup script created ✓"
}

# Display deployment summary
show_deployment_summary() {
    local nas_ip=$(hostname -I | awk '{print $1}' 2>/dev/null || echo "localhost")
    
    echo ""
    echo "=========================================="
    echo "🎉 DEPLOYMENT COMPLETED SUCCESSFULLY! 🎉"
    echo "=========================================="
    echo ""
    echo "📋 Service Information:"
    echo "  • Web Interface: http://${nas_ip}"
    echo "  • API Endpoint:  http://${nas_ip}:5089" 
    echo "  • Hangfire Dashboard: http://${nas_ip}:5089/hangfire"
    echo ""
    echo "📁 Directories:"
    echo "  • Install: ${INSTALL_DIR}"
    echo "  • Data:    ${DATA_DIR}"
    echo "  • Logs:    ${LOG_DIR}"
    echo "  • Config:  ${CONFIG_DIR}"
    echo ""
    echo "🔧 Management Commands:"
    echo "  • View logs:    docker-compose -f ${INSTALL_DIR}/source/Delivery/docker-compose.yml logs -f"
    echo "  • Restart:      docker-compose -f ${INSTALL_DIR}/source/Delivery/docker-compose.yml restart"
    echo "  • Stop:         docker-compose -f ${INSTALL_DIR}/source/Delivery/docker-compose.yml down"
    echo "  • Update:       $0"
    echo "  • Backup:       ${INSTALL_DIR}/backup.sh"
    echo ""
    echo "📊 Container Status:"
    docker-compose -f "${INSTALL_DIR}/source/Delivery/docker-compose.yml" ps
    echo ""
    echo "✅ FileCategorization is now running on your QNAP NAS!"
    echo "🌐 Access the web interface at: http://${nas_ip}"
    echo ""
}

# Main execution
main() {
    log "INFO" "Starting $SCRIPT_NAME v$SCRIPT_VERSION"
    log "INFO" "Target: QNAP ARM32 NAS Deployment"
    echo ""
    
    check_prerequisites
    setup_directories
    clone_or_update_repo
    generate_production_config
    build_docker_images
    deploy_containers
    create_system_service
    create_backup_script
    
    show_deployment_summary
    
    log "INFO" "Deployment completed successfully! 🎉"
}

# Script execution with error handling
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi