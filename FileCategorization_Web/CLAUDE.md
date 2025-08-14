# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Blazor WebAssembly** application for file categorization management. The application provides a web interface to interact with a backend file categorization service that handles machine learning-based file categorization, configuration management, and file operations.

## Development Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run the application (development mode)
dotnet run

# Run with specific profile
dotnet run --launch-profile https
```

### Testing and Validation
```bash
# Build for production
dotnet build --configuration Release

# Publish the application
dotnet publish --configuration Release
```

## Architecture

### Core Structure
- **Blazor WebAssembly App**: Client-side application running .NET 8.0
- **Service Layer**: HTTP client services communicating with external REST APIs
- **Component Structure**: Razor pages organized by feature areas
- **Dependency Injection**: Scoped services registered in Program.cs

### Key Services
- `FileCategorizationServices.cs:11` - Main service for file operations, categorization, and ML model training
- `WebScrumServices.cs` - Secondary service for web scraping functionality
- Both implement interfaces in `/Interfaces/` directory

### API Integration
The application communicates with a backend service via REST API:
- Base URL configured in `wwwroot/appsettings.json:9` (`Uri` property)
- All API endpoints use `/api/v1/` versioning pattern
- Services handle JSON serialization/deserialization with camelCase naming policy

### UI Framework
- **Radzen Blazor Components**: Primary UI component library
- Services registered: DialogService, NotificationService, TooltipService, ContextMenuService
- Custom CSS in `wwwroot/css/app.css`

### Page Structure
- `Pages/FileCategorization/` - Main feature pages
  - `FileCategorizationIndex.razor` - File listing and management
  - `Config.razor` - Configuration management
  - `LastView.razor` - Recent files view
  - `WebScrum.razor` - Web scraping interface
- `Pages/Home.razor` - Landing page

### Data Models
- DTOs in `Data/DTOs/FileCategorizationDTOs/` for API communication
- `FilesDetailDto`, `ConfigsDto`, `FileMoveDto` are primary data transfer objects
- Enums in `Data/Enum/` for typed constants

## Configuration

### Application Settings
- Development: `wwwroot/appsettings.Development.json`
- Production: `wwwroot/appsettings.json`
- Key setting: `Uri` for backend API base address

### Launch Profiles
- HTTP: localhost:5045
- HTTPS: localhost:7275 (SSL), localhost:5045 (fallback)
- IIS Express: localhost:16231 (SSL: 44379)

## Modern Architecture Implementation (Current Status)

### ✅ Implemented Features

#### **Result Pattern & Error Handling**
- **Result<T>** type implemented in `Data/Common/Result.cs`
- Structured error handling with exception context
- Graceful fallbacks for legacy services

#### **HttpClientFactory Migration**
- Modern HTTP client lifecycle management
- Configuration through `Extensions/ServiceCollectionExtensions.cs`
- Proper dependency injection with scoped lifetimes

#### **Service Architecture**
- **ModernFileCategorizationService**: New service with structured logging
- **LegacyServiceAdapter**: Backward compatibility adapter
- **ServiceCompatibilityExtensions**: Extension methods for seamless transition

#### **Configuration Management**
- **FileCategorizationApiOptions**: Strongly-typed configuration
- Options pattern implementation with validation support
- Environment-aware settings

### 🔄 Architecture Modes

The application supports dual-mode operation:

#### **Modern Mode** (when FileCategorizationApi config exists):
```json
{
  "FileCategorizationApi": {
    "BaseUrl": "http://192.168.1.5:30119/",
    "Timeout": "00:00:30",
    "RetryPolicy": {
      "MaxRetries": 3,
      "BackoffMultiplier": 2
    }
  }
}
```

#### **Legacy Mode** (automatic fallback):
Uses original FileCategorizationServices with adapter pattern.

---

# 📋 Development Roadmap

## 🎯 Phase 1: Foundation ✅ COMPLETED

### **1.1 Result Pattern Implementation** ✅
- `Data/Common/Result.cs` - Result pattern with success/error states
- Extension methods for backward compatibility
- Structured error responses with context

### **1.2 HTTP Client Factory Migration** ✅
- HttpClientFactory configuration in `Extensions/ServiceCollectionExtensions.cs`
- Legacy adapter for gradual migration
- Proper dependency injection setup

## 🛡️ Phase 2: Resilience 🔄 IN PROGRESS

### **2.1 Polly Integration** 🔄 FOUNDATION READY
**Status**: Foundation prepared, policies ready for server-side scenarios
**Files**: Polly packages added, structure ready
**Implementation**:
```csharp
services.AddHttpClient<IFileCategorizationService, ModernFileCategorizationService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

### **2.2 Configuration Modernization** ✅
- Options pattern implemented
- Configuration validation ready
- Environment-specific configurations

## 🏗️ Phase 3: State Management ❌ PLANNED

### **3.1 Fluxor State Management** ❌ TO IMPLEMENT
**Priority**: High | **Effort**: 1 week

**Implementation Plan**:
```bash
# Add Fluxor packages
dotnet add package Fluxor.Blazor.Web

# Create structure
mkdir Features/FileManagement/Store
mkdir Features/FileManagement/Actions
mkdir Features/FileManagement/Reducers
```

**Files to Create**:
- `Features/FileManagement/Store/FileState.cs`
- `Features/FileManagement/Actions/FileActions.cs`
- `Features/FileManagement/Reducers/FileReducers.cs`
- `Features/FileManagement/Effects/FileEffects.cs`

**Benefits**:
- Centralized state management
- Predictable state updates
- Time-travel debugging
- Better component isolation

### **3.2 SignalR Service Refactoring** ❌ TO IMPLEMENT  
**Priority**: High | **Effort**: 3 days

**Current Issue**: SignalR logic embedded in `FileCategorizationIndex.razor:177-225`

**Proposed Structure**:
```csharp
public interface INotificationService : IAsyncDisposable
{
    Task StartAsync();
    Task StopAsync();
    event Action<string, decimal> StockNotificationReceived;
    event Action<int, string, MoveFilesResults> MoveFileNotificationReceived;
}
```

**Files to Create**:
- `Services/SignalR/INotificationService.cs`
- `Services/SignalR/SignalRNotificationService.cs`
- `Extensions/SignalRServiceExtensions.cs`

## 🚀 Phase 4: Advanced Features ❌ PLANNED

### **4.1 Caching Layer** ❌ TO IMPLEMENT
**Priority**: Medium | **Effort**: 3 days

**Implementation**:
```csharp
services.AddMemoryCache();
services.AddScoped<ICacheService, MemoryCacheService>();
```

**Files to Create**:
- `Services/Caching/ICacheService.cs`
- `Services/Caching/MemoryCacheService.cs`
- `Extensions/CachingExtensions.cs`

### **4.2 Testing Infrastructure** ❌ TO IMPLEMENT
**Priority**: Medium | **Effort**: 1 week

**Projects to Create**:
- `FileCategorization_Web.Tests` (Unit tests)
- `FileCategorization_Web.IntegrationTests` (Integration tests)

**Test Framework**:
```csharp
[TestFixture]
public class FileCategorizationServiceTests
{
    private Mock<HttpClient> _httpClientMock;
    private ModernFileCategorizationService _service;
    
    [Test]
    public async Task GetFileListAsync_ReturnsSuccess_WhenApiRespondsOk() { }
}
```

## 📊 Implementation Priority Matrix

### **Immediate (Next Sprint)**
1. **Fluxor State Management** - Centralize application state
2. **SignalR Service Refactoring** - Improve real-time notifications  
3. **Complete Polly Integration** - Full resilience patterns

### **Short Term (1 Month)**
4. **Caching Implementation** - Performance optimization
5. **Testing Framework** - Quality assurance
6. **Error Boundaries** - Enhanced UX on errors

### **Long Term (2-3 Months)**
7. **Performance Monitoring** - Observability with telemetry
8. **Advanced Patterns** - CQRS, Event Sourcing considerations
9. **PWA Features** - Offline support, push notifications

## 🔧 Quick Implementation Guide

### **Start Fluxor State Management**:
```bash
# 1. Install package
dotnet add package Fluxor.Blazor.Web

# 2. Configure in Program.cs
builder.Services.AddFluxor(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
});

# 3. Add to App.razor
<Fluxor.Blazor.Web.StoreInitializer />
```

### **Implement SignalR Service**:
```bash
# 1. Extract SignalR logic from FileCategorizationIndex.razor
# 2. Create dedicated service with proper lifecycle management
# 3. Register in DI container with singleton lifetime
```

## 💡 Architecture Benefits Achieved

- **🛡️ Resilience**: Structured error handling with detailed context
- **⚡ Performance**: HttpClient pooling and proper resource management
- **🔧 Maintainability**: Clean separation of concerns and modern patterns
- **📱 User Experience**: Graceful fallbacks and informative error messages
- **👨‍💻 Developer Experience**: Structured logging and enhanced debugging
- **🔄 Future-Ready**: Foundation for advanced patterns and scalability