#!/bin/bash

#############################################################################
# FileCategorization API - Esempio di Deployment per QNAP ARM32
# 
# Questo script mostra come utilizzare il deployment script principale
# con configurazioni specifiche per il tuo ambiente
#############################################################################

# Imposta la directory di lavoro
cd /share/CACHEDEV2_DATA/Scripts/FileCategorization

# Esempio 1: Deployment con configurazioni da file .env
echo "=========================================="
echo "Deployment con file di configurazione"
echo "=========================================="

# Carica le configurazioni dal file .env
if [[ -f "deploy_qnap_config.env" ]]; then
    source deploy_qnap_config.env
    echo "✓ Configurazioni caricate da deploy_qnap_config.env"
else
    echo "✗ File deploy_qnap_config.env non trovato!"
    exit 1
fi

# Esegui il deployment
./deploy_qnap_api.sh

echo ""
echo "=========================================="
echo "Deployment completato!"
echo "=========================================="
echo "API URL: http://$(hostname -I | awk '{print $1}'):${HOST_PORT:-30219}"
echo "Hangfire: http://$(hostname -I | awk '{print $1}'):${HOST_PORT:-30219}/hangfire"
echo "=========================================="

# Esempio 2: Deployment con parametri da riga di comando (commentato)
# Puoi decommentare e personalizzare questo esempio

: '
echo "=========================================="
echo "Esempio con parametri da riga di comando"
echo "=========================================="

./deploy_qnap_api.sh \
  --repo "https://github.com/your-username/FileCategorization.git" \
  --branch "main" \
  --port 30219 \
  --name "filecat_api_prod" \
  --image "filecat_api" \
  --jwt-secret "super-secure-32-characters-jwt-secret-key-here" \
  --dd-username "your-download-daemon-username" \
  --dd-password "your-download-daemon-password"
'

# Esempio 3: Deployment per ambiente di testing (commentato)
# Utile per testare nuove versioni prima del deployment in produzione

: '
echo "=========================================="
echo "Deployment ambiente di testing"
echo "=========================================="

export GITHUB_REPO="https://github.com/your-username/FileCategorization.git"
export GIT_BRANCH="develop"
export CONTAINER_NAME="filecat_api_test"
export HOST_PORT="30220"
export DOCKER_IMAGE_NAME="filecat_api_test"
export DATA_VOLUME="/share/CACHEDEV2_DATA/Storage/Docker/file_categorization_test:/data"

./deploy_qnap_api.sh
'

echo ""
echo "📋 Comandi utili post-deployment:"
echo "  docker logs filecat_api              # Visualizza log"
echo "  docker stats filecat_api             # Statistiche risorse"
echo "  docker restart filecat_api           # Riavvia container"
echo ""