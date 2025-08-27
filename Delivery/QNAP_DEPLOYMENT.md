# QNAP ARM32 NAS Deployment Guide

## FileCategorization - Complete Stack Deployment per NAS QNAP ARM32

Questa guida completa spiega come utilizzare gli script di deployment automatico per installare l'intera piattaforma FileCategorization (API + WEB) su un NAS QNAP ARM32 con 1GB di RAM.

## 🏗️ Stack Completo

Il sistema FileCategorization comprende due componenti principali:

- **FileCategorization API** (Backend): .NET 8 Web API con ML.NET e Hangfire
- **FileCategorization WEB** (Frontend): Blazor WebAssembly con Fluxor e SignalR

Entrambi i componenti sono ottimizzati per architettura ARM32 e risorse limitate (1GB RAM).

## 📋 Requisiti

### Hardware
- **QNAP NAS ARM32** (architettura `armv7l`)
- **RAM**: Minimo 1GB (ottimizzato per questa configurazione)
- **Spazio disco**: Almeno 2GB liberi per l'applicazione

### Software
- **Container Station** installato e abilitato
- **SSH** abilitato per accesso da terminale
- **Docker** configurato tramite Container Station
- **Download Tools** (uno di questi):
  - **Git** (preferito): `opkg install git` 
  - **wget** (di solito già presente)
  - **curl** (alternativa)
- **unzip** (se non hai Git): `opkg install unzip`

## 🚀 Installazione Rapida - Stack Completo

### Step 1: Download degli script di deployment

Accedi al tuo NAS QNAP via SSH e scarica gli script:

```bash
# Crea directory per il deployment
mkdir -p /share/CACHEDEV2_DATA/Scripts/FileCategorization
cd /share/CACHEDEV2_DATA/Scripts/FileCategorization

# Scarica gli script di deployment
wget https://raw.githubusercontent.com/chim331u/FileCategorization/DeliveryNasArm32/Delivery/deploy_qnap_api.sh
wget https://raw.githubusercontent.com/chim331u/FileCategorization/DeliveryNasArm32/Delivery/deploy_qnap_web_fixed.sh

# Rendi eseguibili gli script
chmod +x deploy_qnap_api.sh
chmod +x deploy_qnap_web_fixed.sh
```

### Step 2: Configurazione

Modifica le configurazioni integrate nello script:

```bash
vi deploy_qnap_api.sh
```

**Modifica la sezione di configurazione (linee 45-119):**
- `GITHUB_REPO`: URL del tuo repository GitHub
- `JWT_SECRET`: Chiave segreta di 32+ caratteri
- `DD_USERNAME`: Username DownloadDaemon  
- `DD_PSW`: Password DownloadDaemon

### Step 3: Deployment Stack Completo

**Prima deploya l'API (backend), poi il WEB (frontend):**

```bash
# 1. Deployment API (Backend) - PRIMA
./deploy_qnap_api.sh

# Attendi che l'API sia operativa, poi:
# 2. Deployment WEB (Frontend) - DOPO
./deploy_qnap_web_fixed.sh

# Verifica che entrambi i servizi siano attivi
docker ps | grep filecat
```

**Configurazione personalizzata API:**
```bash
# API con parametri personalizzati
./deploy_qnap_api.sh --jwt-secret "your-secure-secret" --dd-username "myuser"
```

## 📖 Utilizzo Dettagliato

### Configurazione Avanzata

Lo script contiene tutte le configurazioni parametriche integrate (linee 45-119):

```bash
# Repository GitHub
GITHUB_REPO="https://github.com/chim331u/FileCategorization.git"
GIT_BRANCH="DeliveryNasArm32"

# Configurazione Docker
CONTAINER_NAME="filecat_api"
HOST_PORT="30219"

# Volumi QNAP (personalizza questi percorsi)
DATA_VOLUME="/share/CACHEDEV2_DATA/Storage/Docker/file_categorization:/data"
INCOMING_VOLUME="/share/Download/Incoming:/incoming" 
SERIE_VOLUME="/share/Video/Serie:/serie"

# Credenziali applicazione (OBBLIGATORIO modificare)
JWT_SECRET="your-32-characters-long-super-strong-jwt-secret-key"
DD_USERNAME="user"
DD_PSW="password"
```

**Per modificare**: Apri lo script con `vi deploy_qnap_api.sh` e modifica i valori nella sezione configurazione.

### Opzioni da Riga di Comando

Lo script supporta anche parametri diretti:

```bash
# Deployment con parametri specifici
./deploy_qnap_api.sh \
  --repo "https://github.com/user/FileCategorization.git" \
  --branch "main" \
  --port 30219 \
  --name "filecat_api" \
  --jwt-secret "your-secret-key" \
  --dd-username "myuser" \
  --dd-password "mypass"

# Visualizza help completo
./deploy_qnap_api.sh --help
```

### Struttura Directory su NAS

Il deployment creerà automaticamente questa struttura:

```
/share/CACHEDEV2_DATA/Storage/Docker/file_categorization/
├── FileCat.db              # Database principale SQLite
├── Hangfire.db            # Database Hangfire per job background
└── Log/                   # Directory dei log applicazione
    └── FC20241226.log     # Log giornalieri

/share/Download/Incoming/   # File da categorizzare
/share/Video/Serie/         # Serie TV categorizzate
```

## 🔧 Gestione Container

### Comandi Utili Stack Completo

```bash
# Visualizza stato di tutti i container FileCategorization
docker ps | grep filecat

# Visualizza log in tempo reale
docker logs -f filecat_api    # API Backend
docker logs -f filecat_web    # WEB Frontend

# Riavvia container (order matters: API first, then WEB)
docker restart filecat_api
docker restart filecat_web

# Ferma/Avvia stack completo
docker stop filecat_web filecat_api      # Ferma (WEB prima, poi API)
docker start filecat_api filecat_web     # Avvia (API prima, poi WEB)

# Statistiche risorse complete (importante per ARM32)
docker stats filecat_api filecat_web --no-stream
```

### Accesso alle Applicazioni

Dopo il deployment completo, saranno disponibili:

#### API Backend (Porta 30219)
- **API Endpoint**: `http://your-nas-ip:30219`
- **Swagger UI**: `http://your-nas-ip:30219/swagger`
- **Hangfire Dashboard**: `http://your-nas-ip:30219/hangfire`

#### Web Frontend (Porta 30229)
- **Web UI**: `http://your-nas-ip:30229`
- **Interfaccia Utente**: Blazor WebAssembly con gestione file e configurazioni

### Health Check

Verifica che entrambi i servizi siano operativi:

```bash
# Test API Backend (dovrebbe restituire JSON)
curl http://localhost:30219/health

# Test Web Frontend (dovrebbe restituire HTML)
curl http://localhost:30229/

# Verifica connessione WEB → API
curl http://localhost:30219/api/v2/files
```

## 🌐 Deployment WEB Frontend - Configurazione Specifica

### Script di Deployment WEB

Il deployment WEB utilizza uno script specifico ottimizzato per ARM32:

```bash
# Script principale (tutti i fix applicati)
./deploy_qnap_web_fixed.sh

# Alternative disponibili
./deploy_qnap_web.sh                 # Script originale
./qnap_web_deploy_simple.sh         # Versione semplificata
```

### Fix ARM32 per Blazor WebAssembly

Il deployment WEB risolve automaticamente i problemi ARM32:

#### ✅ Problemi Risolti
- **wasm-tools incompatibility**: Rimosso workload non supportato su ARM32
- **Build context errors**: Corretti i path COPY nel dockerfile
- **Dependency resolution**: Gestione corretta progetto FileCategorization_Shared
- **Memory optimization**: Disabilitata compressione Blazor per ARM32

#### 🔧 Ottimizzazioni ARM32 Applicate
```dockerfile
# Dockerfile ottimizzato per ARM32
RUN dotnet publish \
    --configuration Release \
    --output /app/publish \
    --verbosity minimal \
    /p:BlazorEnableCompression=false \
    /p:BlazorEnableTimeZoneSupport=false \
    /p:InvariantGlobalization=true
```

### Configurazione WEB → API

Il WEB si connette automaticamente all'API attraverso:

```json
{
  "Uri": "http://localhost:30219/",
  "FileCategorizationApi": {
    "BaseUrl": "http://localhost:30219/"
  }
}
```

**La configurazione è automatica** - non richiede modifiche manuali.

### Struttura Container WEB

```
Container: filecat_web
Porte: 30229:80
Volume Mappings:
  - Nginx Logs: /var/log/nginx
  - SSL Certs: /etc/nginx/ssl (preparato per HTTPS futuro)
  
Directory Host:
  - /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/logs
  - /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/certs
```

### Gestione Container WEB

```bash
# Verifica stato WEB
docker ps | grep filecat_web

# Log WEB container
docker logs -f filecat_web

# Riavvia WEB
docker restart filecat_web

# Statistiche risorse WEB
docker stats filecat_web

# Accesso diretto al container
docker exec -it filecat_web /bin/sh
```

### Troubleshooting WEB Specifico

#### WEB non si carica
```bash
# Verifica porta 30229
netstat -tlnp | grep :30229

# Verifica configurazione nginx
docker exec -it filecat_web nginx -t

# Ricarica configurazione nginx
docker exec -it filecat_web nginx -s reload
```

#### WEB non si connette all'API
```bash
# Verifica dalla WEB che l'API sia raggiungibile
docker exec -it filecat_web wget -q --spider http://localhost:30219/health
echo $?  # Dovrebbe essere 0

# Verifica configurazione API URL
docker exec -it filecat_web cat /usr/share/nginx/html/appsettings.json
```

#### Problemi di Performance WEB su ARM32
```bash
# Monitora utilizzo risorse
docker stats filecat_web --no-stream

# Verifica cache nginx
docker exec -it filecat_web ls -la /var/cache/nginx/

# Clear cache se necessario
docker exec -it filecat_web rm -rf /var/cache/nginx/*
docker restart filecat_web
```

## ⚡ Ottimizzazioni ARM32

Lo script applica automaticamente ottimizzazioni per NAS ARM32:

### Memory Management
- **Hangfire Storage**: SQLite invece di in-memory (risparmio 60-80% RAM)
- **Connection Pooling**: Pool ridotto per SQLite (5 connessioni max)
- **Queue Polling**: Intervallo aumentato a 15 secondi (vs 1 secondo default)

### Performance Tuning
- **Platform Specification**: `--platform linux/arm/v7` per build ottimizzate
- **Log Retention**: Rotazione automatica log (30 giorni default)
- **Database Optimization**: SQLite con WAL mode per migliori performance

### Resource Monitoring

Monitora l'utilizzo delle risorse:

```bash
# Statistiche container in tempo reale
docker stats filecat_api --no-stream

# Utilizzo memoria sistema
free -h

# Spazio disco
df -h /share/CACHEDEV2_DATA/Storage/Docker/file_categorization/
```

## 🔄 Update e Maintenance

### Update dello Stack Completo

Per aggiornare a una nuova versione:

```bash
# Update completo - ORDINE IMPORTANTE
# 1. Update API (Backend) - PRIMA
./deploy_qnap_api.sh

# 2. Update WEB (Frontend) - DOPO
./deploy_qnap_web_fixed.sh

# Verifica che entrambi i servizi siano attivi
docker ps | grep filecat

# Update con branch/tag specifico
export GIT_BRANCH="v2.1.0"
./deploy_qnap_api.sh
./deploy_qnap_web_fixed.sh
```

### Backup dei Dati

Effettua backup regolare dei database:

```bash
# Backup database principale
cp /share/CACHEDEV2_DATA/Storage/Docker/file_categorization/FileCat.db \
   /share/CACHEDEV2_DATA/Backup/FileCat_$(date +%Y%m%d).db

# Backup database Hangfire
cp /share/CACHEDEV2_DATA/Storage/Docker/file_categorization/Hangfire.db \
   /share/CACHEDEV2_DATA/Backup/Hangfire_$(date +%Y%m%d).db
```

### Pulizia Sistema Stack Completo

Per liberare spazio disco:

```bash
# Rimuovi immagini Docker non utilizzate
docker image prune -f

# Rimuovi container fermati
docker container prune -f

# Pulizia log vecchi API (>30 giorni)
find /share/CACHEDEV2_DATA/Storage/Docker/file_categorization/Log/ \
  -name "*.log" -mtime +30 -delete

# Pulizia log vecchi WEB nginx (>30 giorni)  
find /share/CACHEDEV2_DATA/Storage/Docker/file_categorization_web/logs/ \
  -name "*.log" -mtime +30 -delete

# Verifica spazio occupato dal stack
du -sh /share/CACHEDEV2_DATA/Storage/Docker/file_categorization*
```

## 🚨 Troubleshooting

### Problemi Comuni

#### Container non si avvia
```bash
# Verifica log di errore
docker logs filecat_api

# Verifica configurazione
docker inspect filecat_api
```

#### API non risponde
```bash
# Verifica porta sia aperta
netstat -tlnp | grep :30219

# Test interno container
docker exec -it filecat_api curl http://localhost:8080/health
```

#### Problemi memoria su ARM32
```bash
# Monitora utilizzo RAM
watch -n 5 'free -h && echo "---" && docker stats --no-stream'

# Riavvia container se necessario
docker restart filecat_api
```

#### Database corrotti
```bash
# Backup e reset database (ATTENZIONE: perdita dati)
cd /share/CACHEDEV2_DATA/Storage/Docker/file_categorization/
cp FileCat.db FileCat.db.backup
rm FileCat.db Hangfire.db
docker restart filecat_api
```

### Log Analysis

Controlla i log per diagnosticare problemi:

```bash
# Log container Docker
docker logs filecat_api | tail -100

# Log applicazione
tail -f /share/CACHEDEV2_DATA/Storage/Docker/file_categorization/Log/*.log

# Filtra errori
grep -i error /share/CACHEDEV2_DATA/Storage/Docker/file_categorization/Log/*.log
```

## 📋 Quick Reference - Comandi Operativi

### Deployment Stack Completo da Zero

```bash
# Setup iniziale (eseguire una sola volta)
mkdir -p /share/CACHEDEV2_DATA/Scripts/FileCategorization
cd /share/CACHEDEV2_DATA/Scripts/FileCategorization

# Download script
wget https://raw.githubusercontent.com/chim331u/FileCategorization/DeliveryNasArm32/Delivery/deploy_qnap_api.sh
wget https://raw.githubusercontent.com/chim331u/FileCategorization/DeliveryNasArm32/Delivery/deploy_qnap_web_fixed.sh
chmod +x *.sh

# Deployment completo (ordine importante)
./deploy_qnap_api.sh      # 1. API Backend
./deploy_qnap_web_fixed.sh  # 2. WEB Frontend

# Verifica finale
docker ps | grep filecat
curl http://localhost:30219/health  # API
curl http://localhost:30229/        # WEB
```

### Monitoraggio Sistema ARM32

```bash
# Monitoraggio risorse complete
watch -n 30 'echo "=== SYSTEM RESOURCES ==="; free -h; echo "=== DOCKER CONTAINERS ==="; docker stats --no-stream filecat_api filecat_web'

# Quick health check
echo "API Health: $(curl -s http://localhost:30219/health | jq -r '.status' 2>/dev/null || echo 'ERROR')"
echo "WEB Health: $(curl -s -o /dev/null -w '%{http_code}' http://localhost:30229/)"

# Log monitoring
docker logs filecat_api --tail 50 --since 5m
docker logs filecat_web --tail 50 --since 5m
```

### Maintenance Routine Settimanale

```bash
# Script di manutenzione (salva come /share/Scripts/filecat_maintenance.sh)
#!/bin/bash
echo "=== FileCategorization Maintenance $(date) ==="

# Backup databases
cp /share/CACHEDEV2_DATA/Storage/Docker/file_categorization/FileCat.db \
   /share/CACHEDEV2_DATA/Backup/FileCat_$(date +%Y%m%d).db

# Clean old logs
find /share/CACHEDEV2_DATA/Storage/Docker/file_categorization*/logs/ \
  -name "*.log" -mtime +14 -delete

# Clean docker
docker image prune -f
docker container prune -f

# Resource check
echo "Disk usage:"
du -sh /share/CACHEDEV2_DATA/Storage/Docker/file_categorization*
echo "Memory usage:"
docker stats --no-stream filecat_api filecat_web

echo "Maintenance completed!"
```

### Emergency Recovery

```bash
# Recovery completo in caso di problemi
echo "Starting emergency recovery..."

# Stop all
docker stop filecat_web filecat_api 2>/dev/null || true
docker rm filecat_web filecat_api 2>/dev/null || true

# Clean images if corrupted
docker rmi filecat_api_image:latest filecat_web_image:latest 2>/dev/null || true

# Re-deploy from scratch
cd /share/CACHEDEV2_DATA/Scripts/FileCategorization
./deploy_qnap_api.sh
./deploy_qnap_web_fixed.sh

echo "Recovery completed - check http://YOUR-NAS-IP:30229"
```

## 📞 Support

Per supporto e segnalazione bug:
- **Repository**: https://github.com/chim331u/FileCategorization
- **Issues**: https://github.com/chim331u/FileCategorization/issues
- **Branch Deployment**: DeliveryNasArm32

## 📝 Note sulla Sicurezza

- **JWT_SECRET**: Usa sempre una chiave di almeno 32 caratteri
- **Database**: I database SQLite sono accessibili solo dal container
- **Credenziali**: Non committare mai le credenziali nel repository
- **Firewall**: Considera di limitare l'accesso alla porta API (30219) solo dalla rete locale