# 🚀 FileCategorization Delivery - QNAP ARM32 NAS Deployment

This delivery package provides automated deployment for **QNAP ARM32 NAS** systems with optimized Docker containers for both API and Web applications.

## 📋 **System Requirements**

### **QNAP NAS Requirements**
- **Architecture**: ARM32/ARM64 compatible
- **QTS**: 4.5.0 or later with Container Station
- **Memory**: Minimum 2GB RAM (4GB recommended)
- **Storage**: 10GB free space for application + data
- **Network**: Static IP with ports 80, 443, 5089, 7128 available

### **Container Station Prerequisites**
- Docker Engine installed via Container Station
- Git available in Container Station or SSH access
- Internet connectivity for package downloads

## 🏗️ **Architecture Overview**

```
┌─────────────────────────────────────────────────────────────────┐
│                        QNAP NAS (ARM32)                        │
├─────────────────────────────────────────────────────────────────┤
│  Container Station                                              │
│  ┌─────────────────────┐  ┌─────────────────────────────────┐   │
│  │  API Container      │  │        Web Container           │   │
│  │  ┌─────────────────┐│  │  ┌─────────────────────────────┐│   │
│  │  │ .NET 8 Runtime  ││  │  │    Static Files (nginx)    ││   │
│  │  │ SQLite Database ││  │  │    Blazor WASM Bundle      ││   │
│  │  │ Hangfire Jobs   ││  │  │    Optimized for ARM32     ││   │
│  │  │ SignalR Hub     ││  │  └─────────────────────────────┘│   │
│  │  │ ML.NET Models   ││  │                                 │   │
│  │  └─────────────────┘│  └─────────────────────────────────┘   │
│  └─────────────────────┘                                      │
│           ↕                              ↕                     │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Persistent Volumes                         │   │
│  │  /share/Container/filecategorization/                   │   │
│  │  ├── data/          (SQLite DB, ML Models)             │   │
│  │  ├── logs/          (Application Logs)                 │   │
│  │  └── config/        (Configuration Files)              │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## 🐳 **Container Specifications**

### **API Container**
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` (ARM32 compatible)
- **Runtime**: .NET 8.0 ASP.NET Core
- **Database**: SQLite (file-based, no external dependencies)
- **Jobs**: Hangfire with in-memory storage
- **ML**: ML.NET models with ARM32 optimization
- **Ports**: 5089 (HTTP), 7128 (HTTPS)

### **Web Container**  
- **Base Image**: `nginx:alpine` (ARM32 compatible)
- **Content**: Static Blazor WASM files + nginx configuration
- **Optimization**: Compressed assets, ARM32 build
- **Ports**: 80 (HTTP), 443 (HTTPS)

## 🚀 **Quick Start**

```bash
# 1. SSH into QNAP NAS
ssh admin@your-qnap-ip

# 2. Run the automated deployment script
curl -fsSL https://raw.githubusercontent.com/your-username/FileCategorization/main/Delivery/deploy.sh | bash

# Alternative: Download and run locally
wget https://raw.githubusercontent.com/your-username/FileCategorization/main/Delivery/deploy.sh
chmod +x deploy.sh
./deploy.sh

# 3. Access the application
# Web UI: http://your-qnap-ip
# API: http://your-qnap-ip:5089/api/v2
# Hangfire Dashboard: http://your-qnap-ip:5089/hangfire
```

## 📁 **Deployment Files**

### **🚀 Main Deployment Scripts**
- **`deploy_qnap_api.sh`** - **Complete QNAP ARM32 API deployment script** (self-contained with integrated configuration)
- **`deploy_qnap_web.sh`** - **Complete QNAP ARM32 WEB deployment script** (Blazor WebAssembly with nginx)
- **`QNAP_DEPLOYMENT.md`** - **Complete deployment documentation** with troubleshooting and best practices
- `deploy.sh` - Legacy automated deployment script (multi-container setup)

### **🧪 Testing & Development**
- `qnap-deploy-example.sh` - Example usage scenarios for API deployment script
- `test-deploy.sh` - Configuration validation and testing script for API
- `test-deploy-web.sh` - Configuration validation and testing script for WEB
- `test-download.sh` - Repository download testing script

### **🐳 Docker Configuration**
- `docker-compose.yml` - Multi-container orchestration with ARM32 optimization
- **`api.dockerfile`** - **Production API container** (ARM32 optimized, multi-stage build)
- **`api-simple.dockerfile`** - **Simplified API container** (fallback for compatibility issues)
- **`api-minimal.dockerfile`** - **Ultra-minimal API container** (maximum compatibility)
- **`api-optimized.dockerfile`** - **Performance-optimized API container** (warning suppression, faster build)
- `web.dockerfile` - Static file serving container with nginx (ARM32)

### **⚙️ Configuration Files**
- `nginx.conf` - Performance optimized nginx configuration
- `nginx-site.conf` - Complete site configuration with API proxy and SignalR
- `appsettings.Production.json` - Production API settings with security and performance

## 🎯 **Recommended Deployment Approach**

### **For QNAP NAS ARM32 (Recommended)**

#### **Step 1: Deploy API First**
```bash
# Download API deployment script
wget https://raw.githubusercontent.com/your-repo/FileCategorization/main/Delivery/deploy_qnap_api.sh
chmod +x deploy_qnap_api.sh

# Edit configuration (lines 45-119)
vi deploy_qnap_api.sh

# Deploy API
./deploy_qnap_api.sh
```

#### **Step 2: Deploy WEB Frontend**
```bash
# Download WEB deployment script
wget https://raw.githubusercontent.com/your-repo/FileCategorization/main/Delivery/deploy_qnap_web.sh
chmod +x deploy_qnap_web.sh

# Edit configuration (lines 45-85) - Make sure API_BASE_URL points to your API
vi deploy_qnap_web.sh

# Deploy WEB
./deploy_qnap_web.sh
```

### **For Advanced Multi-Container Setup**
```bash
# Use docker-compose for full stack deployment
wget https://raw.githubusercontent.com/your-repo/FileCategorization/main/Delivery/deploy.sh
chmod +x deploy.sh
./deploy.sh
```

## 🔧 **Manual Deployment**

If you prefer manual deployment or need to customize the process:

### Prerequisites Setup
```bash
# Ensure Docker and required tools are installed
apk add docker docker-compose git curl

# Enable and start Docker
systemctl enable docker
systemctl start docker
```

### Step-by-Step Manual Process
```bash
# 1. Create directory structure
mkdir -p /share/Container/filecategorization/{data,logs,config,certs}

# 2. Clone repository
git clone https://github.com/your-username/FileCategorization.git /share/Container/filecategorization/source

# 3. Build images
cd /share/Container/filecategorization/source
docker build -f Delivery/api.dockerfile -t filecategorization-api:latest .
docker build -f Delivery/web.dockerfile -t filecategorization-web:latest .

# 4. Deploy with Docker Compose
cd Delivery
docker-compose up -d

# 5. Verify deployment
curl http://localhost/health
curl http://localhost:5089/health
```

## 📊 **Performance Optimizations**

- **ARM32 Native Compilation**: Multi-stage builds for optimal binary size
- **Alpine Linux**: Minimal footprint containers (~50MB vs ~200MB)
- **Static File Optimization**: Gzip compression, cache headers
- **Database Optimization**: SQLite with ARM32 optimized queries
- **Memory Management**: Optimized .NET GC settings for ARM32

## 🛡️ **Security Features**

- **HTTPS Termination**: SSL/TLS support with Let's Encrypt
- **Network Isolation**: Container-to-container communication only
- **File Permissions**: Non-root execution with proper user mapping
- **Secret Management**: Environment-based configuration

## 📈 **Monitoring & Logs**

- **Health Checks**: Automatic container health monitoring
- **Log Aggregation**: Centralized logging to `/share/Container/filecategorization/logs/`
- **Performance Metrics**: Built-in .NET metrics and Hangfire dashboard

## 🔄 **Backup & Recovery**

- **Database Backup**: SQLite file-based backup scripts
- **Configuration Backup**: Settings and ML models backup
- **Container Updates**: Rolling update strategy with zero downtime