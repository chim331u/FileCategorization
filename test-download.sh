#!/bin/bash

# Test script per verificare il download del repository
source deploy_qnap_config.env

echo "Testing repository download..."
echo "Repository: $GITHUB_REPO"  
echo "Branch: $GIT_BRANCH"

# Test ZIP URL creation
repo_base=$(echo "$GITHUB_REPO" | sed 's/\.git$//')
zip_url="${repo_base}/archive/refs/heads/${GIT_BRANCH}.zip"
echo "ZIP URL: $zip_url"

# Test if ZIP URL is accessible
echo "Testing ZIP URL accessibility..."
if command -v curl >/dev/null 2>&1; then
    echo "Using curl to test URL..."
    curl -I "$zip_url"
elif command -v wget >/dev/null 2>&1; then
    echo "Using wget to test URL..."
    wget --spider "$zip_url"
fi

# Test download in a temp directory
TEMP_TEST_DIR="/tmp/test_download"
rm -rf "$TEMP_TEST_DIR"
mkdir -p "$TEMP_TEST_DIR"
cd "$TEMP_TEST_DIR"

echo "Downloading to test directory: $TEMP_TEST_DIR"
if command -v curl >/dev/null 2>&1; then
    curl -L -o "test.zip" "$zip_url"
elif command -v wget >/dev/null 2>&1; then
    wget -O "test.zip" "$zip_url"
fi

if [[ -f "test.zip" ]]; then
    echo "Download successful, ZIP file size:"
    ls -lh test.zip
    
    echo "Testing extraction..."
    unzip -q "test.zip"
    
    echo "Extracted contents:"
    ls -la
    
    # Look for Dockerfile
    echo "Searching for Dockerfile:"
    find . -name "*dockerfile*" -o -name "Dockerfile*"
    
    echo "Directory structure:"
    find . -type d | head -10
else
    echo "Download failed"
fi

echo "Cleanup test directory..."
cd ..
rm -rf "$TEMP_TEST_DIR"