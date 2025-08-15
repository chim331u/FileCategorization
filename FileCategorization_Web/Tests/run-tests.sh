#!/bin/bash

# FileCategorization_Web Test Runner
# This script runs all tests and generates a coverage report

echo "🧪 FileCategorization_Web Test Suite"
echo "======================================"

# Set script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_DIR"

echo "📁 Project Directory: $PROJECT_DIR"
echo ""

# Check if we're in debug configuration (tests only run in debug)
if [[ "${CONFIGURATION:-Debug}" != "Debug" ]]; then
    echo "⚠️  Tests are only available in Debug configuration"
    echo "   Current configuration: ${CONFIGURATION:-Debug}"
    echo "   Setting CONFIGURATION=Debug"
    export CONFIGURATION=Debug
fi

echo "🔍 Discovering tests..."
dotnet test --list-tests --verbosity quiet

echo ""
echo "🚀 Running Unit Tests..."
echo "------------------------"
dotnet test --filter "FullyQualifiedName~Unit" \
    --logger "console;verbosity=normal" \
    --collect:"XPlat Code Coverage" \
    --results-directory:"Tests/TestResults"

if [ $? -ne 0 ]; then
    echo "❌ Unit tests failed"
    exit 1
fi

echo ""
echo "🔗 Running Integration Tests..."
echo "--------------------------------"
dotnet test --filter "FullyQualifiedName~Integration" \
    --logger "console;verbosity=normal" \
    --collect:"XPlat Code Coverage" \
    --results-directory:"Tests/TestResults"

if [ $? -ne 0 ]; then
    echo "❌ Integration tests failed"
    exit 1
fi

echo ""
echo "📊 Running All Tests with Coverage..."
echo "-------------------------------------"
dotnet test \
    --configuration Debug \
    --logger "console;verbosity=normal" \
    --collect:"XPlat Code Coverage" \
    --results-directory:"Tests/TestResults" \
    --settings:"Tests/coverlet.runsettings"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ All tests passed successfully!"
    echo ""
    echo "📈 Coverage reports generated in: Tests/TestResults"
    echo "🎯 Test Summary:"
    echo "   - Unit Tests: Services, Effects, Reducers"
    echo "   - Integration Tests: Caching, Fluxor State Management"
    echo "   - Coverage: Code coverage analysis available"
    echo ""
    echo "💡 To view detailed coverage:"
    echo "   1. Install reportgenerator: dotnet tool install -g dotnet-reportgenerator-globaltool"
    echo "   2. Generate HTML report: reportgenerator -reports:Tests/TestResults/**/coverage.cobertura.xml -targetdir:Tests/CoverageReport"
    echo "   3. Open: Tests/CoverageReport/index.html"
    
    exit 0
else
    echo ""
    echo "❌ Some tests failed"
    echo "📋 Check the output above for details"
    exit 1
fi