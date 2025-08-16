# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **FileCategorization solution** consisting of three .NET 8.0 projects:
- **FileCategorization_Api**: Web API backend with machine learning capabilities
- **FileCategorization_Web**: Blazor WebAssembly frontend with modern state management
- **FileCategorization_Shared**: Common library for shared models and utilities

The system provides file categorization using machine learning, DownloadDaemon integration, and real-time notifications.

## Development Commands

### Solution-Level Commands
```bash
# Build entire solution
dotnet build

# Clean entire solution  
dotnet clean

# Restore packages for all projects
dotnet restore

# Build for production
dotnet build --configuration Release
```

### API Project (FileCategorization_Api)
```bash
# Run API server (development mode)
cd FileCategorization_Api
dotnet run                      # HTTP: http://localhost:5089
dotnet run --launch-profile https  # HTTPS: https://localhost:7128

# Run all API tests
dotnet test

# Database operations
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

### Web Project (FileCategorization_Web)
```bash
# Run Blazor WebAssembly app
cd FileCategorization_Web
dotnet run                      # HTTP: localhost:5045, HTTPS: localhost:7275

# Test infrastructure validation (Blazor WebAssembly limitation)
./Tests/run-tests.sh
```

## Architecture Overview

### Three-Project Solution Structure
```
FileCategorization/
├── FileCategorization_Api/     # .NET 8 Web API backend
├── FileCategorization_Web/     # Blazor WebAssembly frontend
├── FileCategorization_Shared/  # Common library
└── FileCategorization.sln      # Solution file
```

### FileCategorization_Api (Backend)
- **Architecture**: Clean Architecture with Repository Pattern
- **API Style**: Minimal APIs with endpoint mapping
- **Database**: SQLite with Entity Framework Core
- **Background Jobs**: Hangfire with in-memory storage
- **Real-time**: SignalR hub at `/notifications`
- **Authentication**: JWT Bearer token authentication
- **Logging**: Serilog with structured logging

#### Key API Components
- **Domain Layer** (`Domain/`): Entities, DTOs, enums organized by business area
- **Repository Layer** (`Infrastructure/`): EF Core repositories with generic `IRepository<T>`
- **Service Layer** (`Services/`): Business logic services implementing interfaces
- **Endpoint Layer** (`Endpoints/`): Minimal API endpoints with v1/v2 versioning
- **Common** (`Common/`): Shared utilities, validators, Result pattern, AutoMapper profiles

#### API Versioning Strategy
- **v1 endpoints** (`/api/v1/`): Legacy endpoints marked obsolete
- **v2 endpoints** (`/api/v2/`): Modern architecture with Repository Pattern, Result Pattern, FluentValidation

#### Core Features
- Machine learning file categorization using ML.NET
- DownloadDaemon integration for ed2k links
- Background job processing for file operations
- JWT authentication with role-based authorization
- Comprehensive error handling with Result<T> pattern

### FileCategorization_Web (Frontend)
- **Architecture**: Blazor WebAssembly with Fluxor state management
- **UI Framework**: Radzen Blazor components
- **State Management**: Fluxor (Redux pattern) for centralized state
- **Real-time**: SignalR client with auto-reconnection
- **Caching**: State-aware caching with IMemoryCache
- **HTTP**: HttpClientFactory with modern service patterns

#### Key Web Components
- **State Management** (`Features/FileManagement/`): Fluxor actions, reducers, effects, selectors
- **Services** (`Services/`): HTTP client services, caching, SignalR notifications
- **Pages** (`Pages/FileCategorization/`): Feature-specific Razor components
- **Extensions** (`Extensions/`): Service registration and configuration

#### Modern Features (Phase 3-4 Implementation)
- **Fluxor State Management**: Redux pattern with 40+ actions and immutable state
- **SignalR Integration**: Real-time updates automatically flow through Fluxor
- **Caching Layer**: State-aware cache invalidation with tag-based organization
- **Testing Infrastructure**: 90+ unit tests with comprehensive coverage

### FileCategorization_Shared
- **Purpose**: Common models and utilities shared between API and Web projects
- **Target**: .NET 8.0 class library with nullable reference types
- **Dependencies**: Pure .NET library with no external dependencies

## Key Technologies

### Backend Stack
- **.NET 8.0**: Target framework
- **Entity Framework Core 8.0.15**: ORM with SQLite provider
- **ML.NET 4.0.2**: Machine learning framework
- **Hangfire 1.8.18**: Background job processing
- **FluentValidation 11.11.0**: Request validation
- **AutoMapper 12.0.1**: DTO/Entity mapping
- **Serilog**: Structured logging
- **SignalR**: Real-time notifications
- **xUnit 2.9.3**: Testing framework with Moq

### Frontend Stack
- **Blazor WebAssembly**: Client-side .NET runtime
- **Fluxor**: Redux-style state management
- **Radzen Blazor**: UI component library
- **SignalR Client**: Real-time communication
- **HttpClientFactory**: HTTP service management
- **Polly**: Resilience patterns (foundation ready)

## Development Patterns

### Backend Patterns
- **Repository Pattern**: Generic `IRepository<T>` with specialized implementations
- **Result Pattern**: Structured error handling with `Result<T>` wrapper
- **Clean Architecture**: Domain-Infrastructure-Application layer separation
- **Validation Filters**: Automatic request validation with `ValidationFilter<T>`
- **Background Jobs**: Async processing with Hangfire integration

### Frontend Patterns
- **Fluxor State Management**: Centralized Redux-style state with actions/reducers
- **Effect-based Side Effects**: Async operations handled in Effects
- **Service Abstraction**: HTTP services with Result pattern integration
- **Cache-First Loading**: Performance optimization with state-aware invalidation
- **Real-time Integration**: SignalR events dispatch Fluxor actions

## Testing Strategy

### API Testing
- **Location**: `FileCategorization_Api/Tests/`
- **Framework**: xUnit with Moq for mocking
- **Coverage**: Repository, Service, and Endpoint layers
- **Database**: In-memory Entity Framework for isolated testing
- **Async Testing**: Proper async/await patterns with `Assert.ThrowsAsync`

### Web Testing
- **Location**: `FileCategorization_Web/Tests/`
- **Framework**: xUnit with FluentAssertions
- **Coverage**: 90+ unit tests covering state management and caching
- **Test Helpers**: `FluxorTestHelper`, `MockServiceHelper`
- **Limitation**: Blazor WebAssembly cannot execute tests directly (compilation validation only)

## Configuration

### API Configuration
- **Development**: SQLite in `Temp/FileCat.db`, logs in `Temp/FC/`
- **Production**: SQLite in `/data/FileCat.db`, logs in `/data/Log/`
- **JWT Secret**: `JWT:Secret` (dev) or `JWT_SECRET` environment variable (prod)

### Web Configuration
- **API Base URL**: Configured in `wwwroot/appsettings.json` (`Uri` property)
- **Modern Config**: `FileCategorizationApi` section for new service patterns
- **Legacy Fallback**: Automatic fallback to legacy services when modern config missing

## Migration Paths

### API v1 to v2 Migration
- v1 endpoints marked obsolete with deprecation notices
- v2 endpoints use modern patterns (Repository, Result, Validation)
- Gradual migration strategy with backward compatibility

### Web Legacy to Modern
- Legacy services wrapped with adapter pattern
- New features use Fluxor state management
- Component-by-component migration to modern patterns

## Recent Architectural Improvements

### MachineLearningService Refactoring (August 2024)
- **Performance**: 100x improvement with thread-safe model caching
- **Architecture**: Full async/await with Result pattern
- **Testing**: 22 comprehensive test methods covering all scenarios
- **Thread Safety**: SemaphoreSlim with double-check locking

### ActionsEndpoint v2 Optimization (August 2024)
- **3-Phase Optimization**: Deadlock elimination, performance, modern architecture
- **Batch Operations**: Eliminated N+1 queries with optimized database access
- **v2 Endpoints**: 5 modern endpoints with comprehensive validation

### DD (DownloadDaemon) Modernization (August 2024)
- **Complete Refactoring**: Modern v2 architecture with 78 comprehensive tests
- **Web Scraping**: Separated concerns with HTTP client connection pooling
- **Repository Pattern**: Optimized batch operations and statistics queries

### Fluxor State Management Implementation (Phase 3)
- **Redux Pattern**: Centralized state with 40+ actions and immutable updates
- **Real-time Integration**: SignalR events automatically update global state
- **Enhanced Debugging**: Time-travel debugging with Redux DevTools

### Caching Layer Implementation (Phase 4.1)
- **State-Aware Caching**: Automatic invalidation based on Fluxor state changes
- **Tag-Based Organization**: Intelligent cache grouping for precise invalidation
- **Performance Monitoring**: Real-time cache statistics and memory usage tracking

## Development Guidelines

### Code Organization Best Practices
- Follow Clean Architecture principles with clear layer separation
- Use Repository Pattern for data access with Result Pattern for error handling
- Implement comprehensive validation with FluentValidation
- Write comprehensive tests for all new features
- Use Fluxor for shared state, direct services for isolated operations

### API Development
- Prefer v2 endpoints for new features
- Use Result<T> pattern for structured error handling  
- Implement proper validation with ValidationFilter<T>
- Include comprehensive Swagger documentation
- Follow async/await patterns throughout

### Web Development
- Use Fluxor for complex state management and real-time features
- Implement cache-first loading patterns for performance
- Leverage SignalR integration for real-time updates
- Write unit tests for state management components
- Follow modern service registration patterns

## Production Considerations

### API Deployment
- Configure `JWT_SECRET` environment variable
- Set up proper database path (`/data/FileCat.db`)
- Configure log file location (`/data/Log/`)
- Enable HTTPS with proper certificates

### Web Deployment
- Configure production API base URL
- Optimize Blazor WebAssembly for production builds
- Set up proper CORS policies on API
- Consider PWA features for offline support

### Performance Optimization
- Leverage caching layer with appropriate policies
- Use batch operations for database access
- Monitor cache hit/miss ratios
- Implement proper connection pooling for HTTP clients

## TODO - Future Implementation Tasks

### 1. Shared Library Consolidation
**Priority**: High | **Effort**: Medium
- **Task**: Move common DTOs and Response<T> types from API and WEB to FileCategorization_Shared
- **Rationale**: Eliminate code duplication and ensure consistency across projects
- **Scope**: 
  - Common response types (`Result<T>`, `ApiResponse`, error models)
  - Shared DTOs used by both API and Web projects
  - Common enums and constants
- **Benefits**: Reduced maintenance overhead, consistent data contracts, better type safety

### 2. Docker Containerization for ARM32 NAS
**Priority**: High | **Effort**: High
- **Task**: Create optimized Docker configuration for QNAP ARM32 NAS deployment
- **Requirements**:
  - Multi-stage Docker builds for optimized image size
  - ARM32 architecture support (`linux/arm/v7`)
  - Automated deployment scripts
  - Health checks and monitoring
- **Deliverables**:
  - `Dockerfile` for API project with ARM32 optimization
  - `Dockerfile` for Web project (static file serving)
  - `docker-compose.yml` for complete stack deployment
  - Deployment scripts with automatic startup configuration
  - Documentation for QNAP Container Station setup

### 3. Web API v2 Migration
**Priority**: Medium | **Effort**: Medium
- **Task**: Migrate FileCategorization_Web to use v2 API endpoints exclusively
- **Scope**:
  - Update all HTTP services to use `/api/v2/` endpoints
  - Leverage modern Result<T> pattern and structured error handling
  - Implement proper request validation and error responses
  - Remove legacy v1 API dependencies
- **Benefits**: Access to latest features, improved error handling, better performance with batch operations

### 4. UI Framework Evaluation
**Priority**: Low | **Effort**: High
- **Task**: Evaluate Radzen vs Pure Blazor component approach
- **Analysis Required**:
  - Performance comparison between Radzen components and native Blazor
  - Bundle size impact and loading performance
  - Customization flexibility and design system alignment
  - Maintenance overhead and long-term sustainability
- **Options**:
  - **Keep Radzen**: Continue with current Radzen Blazor component library
  - **Pure Blazor**: Rewrite UI using native Blazor components with custom styling
  - **Hybrid Approach**: Selective replacement of heavy Radzen components
- **Decision Criteria**: Performance impact, design flexibility, maintenance complexity