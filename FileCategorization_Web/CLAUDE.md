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

## 🛡️ Phase 2: Resilience ✅ COMPLETED

### **2.1 Polly Integration** 🔄 FOUNDATION READY
**Status**: Foundation prepared, packages added, ready for server-side scenarios
**Files**: Polly packages in place, retry/circuit breaker structure ready
**Note**: Full implementation pending for specific server-side patterns

### **2.2 Configuration Modernization** ✅
- Options pattern implemented with `FileCategorizationApiOptions`
- Configuration validation and type safety
- Environment-specific configurations with dual-mode support

## 🏗️ Phase 3: State Management ✅ COMPLETED

### **3.1 Fluxor State Management** ✅ IMPLEMENTED
**Status**: Full Redux pattern implementation complete
**Priority**: High | **Effort**: Completed

**Implementation Completed**:
```bash
# ✅ Structure Created
Features/FileManagement/Store/FileState.cs
Features/FileManagement/Actions/FileActions.cs  
Features/FileManagement/Reducers/FileReducers.cs
Features/FileManagement/Effects/FileEffects.cs
```

**Features Implemented**:
- **Immutable State**: Complete application state with files, categories, configurations
- **40+ Actions**: Typed actions for all operations (files, ML, SignalR events)
- **Pure Reducers**: State transition logic with immutable updates
- **Async Effects**: Side effects handling with API calls and error management
- **Selectors**: Computed state for filtered views and derived data

**Benefits Achieved**:
- ✅ Centralized state management across entire application
- ✅ Predictable state updates through action dispatch
- ✅ Time-travel debugging with Redux DevTools support
- ✅ Enhanced component isolation and testability
- ✅ Real-time state synchronization with SignalR events

### **3.2 SignalR Service Refactoring** ✅ IMPLEMENTED
**Status**: Complete professional SignalR architecture
**Priority**: High | **Effort**: Completed

**Architecture Implemented**:
```csharp
// ✅ Professional Service Architecture
public interface INotificationService : IAsyncDisposable
{
    Task StartAsync();
    Task StopAsync(); 
    bool IsConnected { get; }
    string? ConnectionId { get; }
    event Action<string, decimal> StockNotificationReceived;
    event Action<int, string, MoveFilesResults> MoveFileNotificationReceived;
    event Action<string, MoveFilesResults> JobNotificationReceived;
}
```

**Files Implemented**:
- ✅ `Services/SignalR/INotificationService.cs` - Clean service interface
- ✅ `Services/SignalR/SignalRNotificationService.cs` - Full implementation with auto-reconnection
- ✅ `Extensions/SignalRServiceExtensions.cs` - DI registration and configuration

**Advanced Features**:
- ✅ **Auto-Reconnection**: Exponential backoff with resilient connection management
- ✅ **Fluxor Integration**: SignalR events automatically dispatch actions to update global state
- ✅ **Event-Driven Architecture**: Clean separation between SignalR events and application logic
- ✅ **Connection Lifecycle**: Proper startup, shutdown, and disposal handling
- ✅ **Diagnostic Logging**: Comprehensive logging for connection status and events
- ✅ **Memory Management**: Singleton pattern with proper resource cleanup

**Real-time Features**:
- ✅ File move notifications update UI instantly
- ✅ Job completion status reflected in global state
- ✅ Connection status displayed in console messages
- ✅ Automatic state synchronization across components

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
1. ✅ **Fluxor State Management** - COMPLETED: Centralized application state with Redux pattern
2. ✅ **SignalR Service Refactoring** - COMPLETED: Professional real-time notification service  
3. **Complete Polly Integration** - Full resilience patterns for production scenarios

### **Short Term (1 Month)**
4. **Caching Implementation** - State-aware performance optimization with IMemoryCache
5. **Testing Framework** - Unit/integration tests for Fluxor actions, reducers, and effects
6. **Component Migration** - Migrate existing pages to use Fluxor state management
7. **Error Boundaries** - Enhanced UX with centralized error handling

### **Long Term (2-3 Months)**
8. **Performance Monitoring** - Observability with telemetry and action tracking
9. **Advanced Patterns** - CQRS, Event Sourcing with established Fluxor foundation
10. **PWA Features** - Offline support, push notifications, and service workers

## 🔧 Quick Implementation Guide

### **Using Fluxor State Management** ✅ READY:
```csharp
// 1. Inject state and dispatcher in components
@inject IState<FileState> FileState
@inject IDispatcher Dispatcher

// 2. Subscribe to state changes
@if (FileState.Value.IsLoading)
{
    <p>Loading...</p>
}

// 3. Dispatch actions
private void LoadFiles() => Dispatcher.Dispatch(new LoadFilesAction(searchParameter));

// 4. Use selectors for computed state
var filteredFiles = FileStateSelectors.GetFilteredFiles(FileState.Value);
```

### **Using SignalR Service** ✅ READY:
```csharp
// 1. Inject notification service
@inject INotificationService NotificationService

// 2. Start connection in OnInitializedAsync
protected override async Task OnInitializedAsync()
{
    await NotificationService.StartAsync();
}

// 3. Events are automatically integrated with Fluxor state
// No manual event handling needed - state updates automatically!
```

### **Next Phase Development**:
```bash
# 1. Implement caching layer
mkdir Services/Caching
# Create ICacheService and MemoryCacheService

# 2. Add component-level Fluxor integration
# Update FileCategorizationIndex.razor to use Fluxor

# 3. Implement testing framework
mkdir Tests/Features/FileManagement
# Create tests for actions, reducers, and effects
```

## 💡 Architecture Benefits Achieved

### **🏗️ Foundation (Phase 1-2)**
- **🛡️ Resilience**: Structured error handling with Result<T> pattern and detailed context
- **⚡ Performance**: HttpClient pooling and proper resource management
- **🔧 Maintainability**: Clean separation of concerns with modern patterns
- **📱 User Experience**: Graceful fallbacks and informative error messages
- **👨‍💻 Developer Experience**: Structured logging and enhanced debugging

### **🎯 State Management (Phase 3)**
- **📊 Predictable State**: Redux pattern with immutable state management
- **🔄 Real-time Updates**: SignalR events automatically update global state
- **🐛 Enhanced Debugging**: Time-travel debugging with Redux DevTools
- **⚡ Performance Optimization**: Structural sharing and optimized re-renders
- **🧪 Testability**: Pure functions and isolated effects for easy testing
- **🔌 Professional Real-time**: Auto-reconnecting SignalR with event-driven architecture

### **🚀 Ready for Scale**
- **🔄 Future-Ready**: Foundation for advanced patterns (CQRS, Event Sourcing)
- **📈 Scalability**: Centralized state management supports complex features
- **🧩 Modularity**: Feature-based organization with clear boundaries
- **⚙️ Extensibility**: Plugin architecture ready for caching, monitoring, and PWA features

## 🎓 Development Guidance

### **When to Use Fluxor vs Direct Service Calls**
- **Use Fluxor**: For UI state, shared data, real-time updates, complex workflows
- **Use Direct Service**: For simple CRUD operations, one-off API calls, isolated features

### **SignalR Integration Best Practices**
- **Automatic State Updates**: Let SignalR events flow through Fluxor actions
- **Connection Management**: Use the singleton INotificationService
- **Error Handling**: Connection errors are logged and dispatched to state
- **Performance**: Connection pooling handled automatically

### **Migration Strategy**
- **Gradual Adoption**: New features use Fluxor, existing code works unchanged
- **Component-by-Component**: Migrate pages one at a time to Fluxor patterns
- **Backward Compatibility**: Legacy service adapters maintain existing functionality