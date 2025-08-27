# QNAP ARM32 NAS Deployment Guide

## FileCategorization API - Docker Deployment per NAS QNAP ARM32

Questa guida spiega come utilizzare lo script di deployment automatico per installare l'API FileCategorization su un NAS QNAP ARM32 con 1GB di RAM.

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

## 🚀 Installazione Rapida

### Step 1: Download dello script di deployment

Accedi al tuo NAS QNAP via SSH e scarica lo script:

```bash
# Crea directory per il deployment
mkdir -p /share/CACHEDEV2_DATA/Scripts/FileCategorization
cd /share/CACHEDEV2_DATA/Scripts/FileCategorization

# Scarica lo script di deployment
wget https://raw.githubusercontent.com/your-repo/FileCategorization/main/deploy_qnap_api.sh

# Rendi eseguibile lo script
chmod +x deploy_qnap_api.sh
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

### Step 3: Deployment

Esegui il deployment:

```bash
# Deployment diretto (configurazione integrata)
./deploy_qnap_api.sh

# Oppure con parametri personalizzati
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

### Comandi Utili

```bash
# Visualizza stato container
docker ps | grep filecat_api

# Visualizza log in tempo reale
docker logs -f filecat_api

# Riavvia container
docker restart filecat_api

# Ferma container
docker stop filecat_api

# Avvia container fermato
docker start filecat_api

# Statistiche risorse (importante per ARM32)
docker stats filecat_api
```

### Accesso all'API

Dopo il deployment, l'API sarà disponibile su:

- **API Endpoint**: `http://your-nas-ip:30219`
- **Swagger UI**: `http://your-nas-ip:30219/swagger`
- **Hangfire Dashboard**: `http://your-nas-ip:30219/hangfire`

### Health Check

Verifica che l'API sia operativa:

```bash
# Test endpoint di health
curl http://localhost:30219/health

# Test endpoint API
curl http://localhost:30219/api/v2/files
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

### Update dell'Applicazione

Per aggiornare a una nuova versione:

```bash
# Ri-esegui il deployment (scaricherà l'ultima versione)
./deploy_qnap_api.sh

# Oppure specifica un branch/tag specifico
export GIT_BRANCH="v2.1.0"
./deploy_qnap_api.sh
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

### Pulizia Sistema

Per liberare spazio disco:

```bash
# Rimuovi immagini Docker non utilizzate
docker image prune -f

# Rimuovi container fermati
docker container prune -f

# Pulizia log vecchi (>30 giorni)
find /share/CACHEDEV2_DATA/Storage/Docker/file_categorization/Log/ \
  -name "*.log" -mtime +30 -delete
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

## 📞 Support

Per supporto e segnalazione bug:
- **Repository**: https://github.com/your-repo/FileCategorization
- **Issues**: https://github.com/your-repo/FileCategorization/issues
- **Wiki**: https://github.com/your-repo/FileCategorization/wiki

## 📝 Note sulla Sicurezza

- **JWT_SECRET**: Usa sempre una chiave di almeno 32 caratteri
- **Database**: I database SQLite sono accessibili solo dal container
- **Credenziali**: Non committare mai le credenziali nel repository
- **Firewall**: Considera di limitare l'accesso alla porta API (30219) solo dalla rete locale