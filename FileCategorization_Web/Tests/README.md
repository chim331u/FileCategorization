# 🧪 FileCategorization_Web - Test Suite

This directory contains the comprehensive test suite for the FileCategorization_Web application, focusing on testing the modern architecture implementation including Fluxor state management, caching layer, and service integrations.

## 📁 Test Structure

```
Tests/
├── Unit/                   # Unit Tests
│   ├── Services/          # Service layer tests
│   │   ├── MemoryCacheServiceTests.cs
│   │   └── StateAwareCacheServiceTests.cs
│   ├── Effects/           # Fluxor Effects tests
│   │   └── FileEffectsTests.cs
│   └── Reducers/          # Fluxor Reducers tests
│       └── FileReducersTests.cs
├── Integration/           # Integration Tests
│   └── CachingIntegrationTests.cs
├── Helpers/              # Test Utilities
│   ├── FluxorTestHelper.cs
│   └── MockServiceHelper.cs
├── Fixtures/             # Test Data & Setup
├── TestConfiguration.cs  # Global test configuration
├── run-tests.sh         # Test runner script
├── coverlet.runsettings # Coverage configuration
└── README.md           # This file
```

## 🎯 Test Coverage

### **Unit Tests (90+ tests)**
- **MemoryCacheService** (12 tests)
  - Cache hit/miss scenarios
  - Tag-based invalidation
  - Statistics tracking
  - Memory management
  - Error handling

- **StateAwareCacheService** (8 tests)
  - Delegation to base service
  - State validation logic
  - Event forwarding
  - Fluxor integration

- **FileReducers** (25+ tests)
  - All action types covered
  - State immutability verification
  - Console message generation
  - Cache statistics updates
  - SignalR event handling

- **FileEffects** (20+ tests)
  - Cache-first loading patterns
  - API fallback scenarios
  - Error handling
  - Cache invalidation
  - Service interactions

### **Integration Tests (8+ tests)**
- **Caching Integration**
  - Real cache operations
  - Tag-based invalidation
  - Concurrent access patterns
  - Large data handling
  - Expiration policies
  - Statistics accuracy

## 🚀 Running Tests

### **Quick Start**
```bash
# Navigate to project directory
cd FileCategorization_Web

# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal
```

### **Using Test Runner Script**
```bash
# Make script executable (first time only)
chmod +x Tests/run-tests.sh

# Run comprehensive test suite
./Tests/run-tests.sh
```

### **Specific Test Categories**
```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName~Unit"

# Integration tests only
dotnet test --filter "FullyQualifiedName~Integration"

# Specific test class
dotnet test --filter "ClassName=MemoryCacheServiceTests"

# Specific test method
dotnet test --filter "FullName~GetAsync_WhenKeyDoesNotExist_ReturnsNull"
```

## 📊 Test Results & Coverage

### **Expected Results**
- ✅ **90+ Unit Tests**: All passing
- ✅ **8+ Integration Tests**: All passing  
- ✅ **Code Coverage**: 80%+ for tested components
- ⚠️ **Warnings**: Only nullable reference warnings (expected)

### **Coverage Reports**
```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Install report generator (one-time setup)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML coverage report
reportgenerator -reports:Tests/TestResults/**/coverage.cobertura.xml -targetdir:Tests/CoverageReport

# Open coverage report
open Tests/CoverageReport/index.html
```

## 🛠️ Test Development Guide

### **Writing New Tests**

1. **Choose Test Type**:
   - **Unit**: Single class/method testing
   - **Integration**: Multi-component interaction

2. **Use Test Helpers**:
   ```csharp
   // For Fluxor testing
   var serviceProvider = FluxorTestHelper.CreateTestServiceProvider();
   
   // For mock services
   var mockCache = MockServiceHelper.CreateMockCacheService();
   services.AddMockServices();
   ```

3. **Follow Naming Convention**:
   ```csharp
   [Fact]
   public void MethodName_Scenario_ExpectedResult()
   {
       // Arrange
       // Act  
       // Assert
   }
   ```

### **Test Categories Covered**

| Component | Coverage | Test Types |
|-----------|----------|------------|
| **Caching Services** | ✅ Complete | Unit + Integration |
| **Fluxor Reducers** | ✅ Complete | Unit |
| **Fluxor Effects** | ✅ Complete | Unit |
| **State Management** | ✅ Complete | Unit + Integration |
| **Error Handling** | ✅ Complete | Unit |
| **Performance** | ✅ Complete | Integration |

## 🧩 Test Dependencies

The test suite uses the following packages:
- **xUnit**: Test framework
- **Moq**: Mocking framework  
- **FluentAssertions**: Fluent assertion library
- **Microsoft.NET.Test.Sdk**: Test SDK
- **Coverlet**: Code coverage

All test dependencies are configured to load only in Debug configuration to avoid bloating production builds.

## 🎓 Best Practices

### **Unit Test Principles**
- ✅ **Fast**: Sub-second execution
- ✅ **Independent**: No test dependencies
- ✅ **Repeatable**: Consistent results
- ✅ **Self-Validating**: Clear pass/fail
- ✅ **Timely**: Written with/before code

### **Mocking Strategy**
- **External Dependencies**: Always mocked
- **Internal Services**: Mocked for unit tests, real for integration
- **Fluxor State**: Mocked state for isolation
- **Cache Operations**: Mix of mocked and real implementations

### **Assertion Patterns**
```csharp
// Preferred: FluentAssertions
result.Should().NotBeNull();
result.Should().HaveCount(5);
result.Should().BeEquivalentTo(expectedValue);

// Behavioral verification
mockService.Verify(x => x.Method(It.IsAny<string>()), Times.Once);
```

## 🔧 Troubleshooting

### **Common Issues**

1. **"No tests found"**
   - Ensure `--configuration Debug` (tests only available in Debug)
   - Verify test method has `[Fact]` or `[Theory]` attribute

2. **Fluxor initialization errors**
   - Use `FluxorTestHelper.CreateTestServiceProvider()`
   - Avoid browser-specific middleware in tests

3. **Cache test failures**
   - Ensure proper disposal of cache services
   - Use unique cache keys to avoid conflicts

4. **Mock setup issues**
   - Use specific generic types instead of `It.IsAnyType`
   - Setup return values for all expected calls

### **Debug Test Execution**
```bash
# Verbose test output
dotnet test --verbosity diagnostic

# List all discovered tests
dotnet test --list-tests

# Debug specific test
dotnet test --filter "TestName" --logger "console;verbosity=detailed"
```

## 📈 Future Test Expansions

### **Planned Test Additions**
- **Component Tests**: Blazor component rendering tests
- **E2E Tests**: Full user workflow automation  
- **Performance Tests**: Load testing for cache operations
- **API Integration**: Tests against real backend API
- **Browser Tests**: Playwright/Selenium integration

### **Test Infrastructure Improvements**
- **Parallel Execution**: Optimize test performance
- **Custom Test Attributes**: Domain-specific test categorization
- **Test Data Builders**: Simplified test data creation
- **Snapshot Testing**: UI component regression testing

---

## 💡 Quick Reference

```bash
# Most common commands
dotnet test                                    # Run all tests
dotnet test --verbosity normal                # Detailed output  
./Tests/run-tests.sh                         # Full test suite
dotnet test --filter "Unit"                  # Unit tests only
dotnet test --collect:"XPlat Code Coverage"  # With coverage
```

This test suite provides comprehensive coverage of the modern architecture components and serves as a foundation for maintaining code quality as the application evolves.