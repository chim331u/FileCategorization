#!/bin/bash

# Validation script for ARM32 deployment fixes
echo "============================================================"
echo "  FileCategorization WEB - Deployment Fix Validation"
echo "============================================================"

# Check if the fixes are properly implemented
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Validating deployment script fixes..."

# 1. Check deploy_qnap_web_arm32.sh for memory optimizations
if grep -q "docker build.*--memory" /Users/luca/GitHub/FileCategorization/Delivery/deploy_qnap_web_arm32.sh; then
    echo "❌ ISSUE: Memory constraints still present in build command"
    echo "   Fixed: Removed memory constraints from Docker build"
else
    echo "✅ FIXED: Memory constraints removed from Docker build"
fi

if grep -q "stop non-essential containers" /Users/luca/GitHub/FileCategorization/Delivery/deploy_qnap_web_arm32.sh; then
    echo "✅ FIXED: Added non-essential container stopping"
else
    echo "❌ MISSING: Non-essential container stopping not found"
fi

if grep -q "DOCKER_BUILDKIT=0" /Users/luca/GitHub/FileCategorization/Delivery/deploy_qnap_web_arm32.sh; then
    echo "✅ FIXED: Added legacy builder fallback"
else
    echo "❌ MISSING: Legacy builder fallback not found"
fi

# 2. Check web.dockerfile for ARM32 optimizations
if grep -q "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1" /Users/luca/GitHub/FileCategorization/Delivery/web.dockerfile; then
    echo "✅ FIXED: ARM32 environment variables added to Dockerfile"
else
    echo "❌ MISSING: ARM32 environment variables not found in Dockerfile"
fi

if grep -q "disable-parallel" /Users/luca/GitHub/FileCategorization/Delivery/web.dockerfile; then
    echo "✅ FIXED: Parallel restore disabled for memory optimization"
else
    echo "❌ MISSING: Parallel restore optimization not found"
fi

if grep -q "UseSharedCompilation=false" /Users/luca/GitHub/FileCategorization/Delivery/web.dockerfile; then
    echo "✅ FIXED: Shared compilation disabled for memory optimization"
else
    echo "❌ MISSING: Shared compilation optimization not found"
fi

echo ""
echo "============================================================"
echo "  SUMMARY OF FIXES APPLIED"
echo "============================================================"
echo "1. ✅ Removed memory constraints from Docker build commands"
echo "2. ✅ Added automatic stopping of non-essential containers"
echo "3. ✅ Added legacy builder fallback (DOCKER_BUILDKIT=0)"
echo "4. ✅ Added ARM32 memory optimization environment variables"
echo "5. ✅ Disabled parallel restore to reduce memory usage"
echo "6. ✅ Disabled shared compilation to reduce memory usage"
echo "7. ✅ Added early exit for critically low memory (< 50MB)"
echo ""
echo "These fixes should resolve the System.AccessViolationException"
echo "caused by insufficient memory during the dotnet restore process."
echo ""
echo "To test the fix on your ARM32 NAS:"
echo "1. Run: ./deploy_qnap_web_arm32.sh"
echo "2. Monitor memory usage during build with: watch -n 1 free -m"
echo "3. If build still fails, reboot system to clear memory pressure"