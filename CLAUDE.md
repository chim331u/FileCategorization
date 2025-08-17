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
- **v1 endpoints** (`/api/v1/`): Legacy endpoints (deprecated, maintained for compatibility)
- **v2 endpoints** (`/api/v2/`): Modern architecture with Repository Pattern, Result Pattern, FluentValidation
- **Migration Status**: All client applications now use v2 endpoints by default

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
- **API v2 Integration**: All services use modern v2 endpoints with enhanced error handling

### FileCategorization_Shared
- **Purpose**: Common models and utilities shared between API and Web projects
- **Target**: .NET 8.0 class library with nullable reference types
- **Dependencies**: Pure .NET library with no external dependencies
- **DTOs**: Centralized data transfer objects for v2 API contracts (FileManagement, Configuration, DD)

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
- **Service Abstraction**: HTTP services with Result pattern integration and v2 API endpoints
- **Cache-First Loading**: Performance optimization with state-aware invalidation
- **Real-time Integration**: SignalR events dispatch Fluxor actions
- **Service Adapters**: Intelligent selection between modern v2 and legacy v1 APIs based on configuration

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

### UI/UX Enhancements and Bug Fixes (August 2024)
- **Comprehensive Notifications**: Added Radzen NotificationService for all CRUD operations
- **Environment-Based Config**: Fixed Config creation to use IHostEnvironment.IsDevelopment()
- **Cache Invalidation**: Resolved Config grid refresh issues with proper cache bypass
- **Error Handling**: Enhanced timeout detection and detailed error messages for ML operations
- **API Integration**: Fixed 400/500 errors with proper DTO format and request validation
- **Real-time Feedback**: Added UI notifications for Refresh and ForceCategory actions
- **Data Consistency**: Fixed IsDev parameter preservation across all Config operations

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

## 🚀 Sprint Plan - Configuration Management Modernization (August 2024)

### Overview
Piano di modernizzazione per eliminare riferimenti API v1 dal progetto Web, consolidare i DTO delle configurazioni, e automatizzare la gestione dell'environment (dev/prod) senza parametri manuali IsDev.

### Sprint 1: ✅ COMPLETED - Eliminare riferimenti API v1 dal progetto WEB
**Obiettivo**: Rimuovere tutti i riferimenti alle API v1 dal FileCategorization_Web
**Status**: ✅ Completato - La maggior parte del codice legacy era già stata rimossa
**Risultato**: Pulizia delle configurazioni di fallback e conferma uso esclusivo API v2

### Sprint 2: ✅ COMPLETED - Consolidamento DTO Config
**Obiettivo**: Eliminare ConfigDTO e sostituire con ConfigRequest/ConfigResponse/ConfigUpdateRequest in FileCategorization_Shared
**Status**: ✅ Completato - Tutti i DTO centralizzati in FileCategorization_Shared
**Risultato**: 
- Eliminata duplicazione DTO tra progetti
- ConfigRequest, ConfigResponse, ConfigUpdateRequest ora in FileCategorization_Shared
- Mappature AutoMapper aggiornate

### Sprint 3: 🔄 IN PROGRESS - Automazione Environment (IsDev)
**Obiettivo**: Rimuovere IsDev dai DTO Config e automatizzare basandosi su `IHostEnvironment.IsDevelopment()`

#### ✅ Day 1 - Rimozione IsDev dai DTO Shared (COMPLETED)
- ✅ Rimosso IsDev da ConfigRequest.cs
- ✅ Rimosso IsDev da ConfigResponse.cs  
- ✅ Rimosso IsDev da ConfigUpdateRequest.cs
- ✅ Mantenuto IsDev solo nell'Entity Configs.cs (database)

#### ✅ Day 2 - Aggiornamento API Layer (COMPLETED)
- ✅ Aggiornato AutoMapper ConfigProfile.cs per gestire IsDev automaticamente
- ✅ Aggiornato ConfigUpdateRequestValidator per rimuovere validazione IsDev
- ✅ Modificato ConfigEndpoint.cs per impostare IsDev automaticamente tramite IHostEnvironment
- ✅ Aggiornato ConfigRepository.cs per filtrare per environment corrente
- ✅ Testato logica di filtro per environment nell'API

#### ✅ Day 3a - Aggiornamento Web Service Layer (COMPLETED)
- ✅ Aggiornato ModernFileCategorizationService.cs per non inviare IsDev nelle request
- ✅ Corretto mapping ConfigResponse → ConfigsDto rimuovendo riferimenti IsDev
- ✅ Aggiornati test di integrazione per rimuovere riferimenti IsDev

#### ✅ Day 3b - State Management & UI (COMPLETED)
- ✅ Modificato Fluxor state management per rimuovere IsDev
- ✅ Aggiornato UI Config.razor per rimuovere campo IsDev
- ✅ Testato che dev/prod environments funzionino correttamente
- ✅ Validato che configurations siano filtrate correttamente

#### ✅ Sprint 3 Hotfixes (COMPLETED)
- ✅ Aggiunta validazione client-side per campi vuoti in Config.razor
- ✅ **SPECIAL CHARACTER SUPPORT**: Risolto problema gestione caratteri speciali "?" nelle configurazioni
  - Aggiornato regex validator: `^[a-zA-Z0-9._\\-?@#%&*+=\\[\\]()]+$`
  - Supporto completo per caratteri speciali sicuri: `?@#%&*+=[]()` 
  - Messaggio errore aggiornato per documentare caratteri supportati
  - Test end-to-end confermato per chiavi/valori con carattere "?"

### Risultati Ottenuti
#### ✅ API Layer
- **Environment Automation**: Configurazioni automaticamente filtrate per environment corrente
- **Eliminazione IsDev**: Parametro rimosso da tutti i DTO pubblici (request/response)
- **Backward Compatibility**: Entity database mantiene IsDev per compatibilità
- **Auto-Detection**: `IHostEnvironment.IsDevelopment()` utilizzato per determinare environment

#### ✅ Web Service Layer  
- **Modern API Integration**: ModernFileCategorizationService aggiornato per API v2 senza IsDev
- **Test Coverage**: Test di serializzazione aggiornati per nuovo contratto API
- **Clean Architecture**: Rimozione parametri manuali IsDev dai servizi

#### 🎯 Benefici Architetturali
- **Meno Errori**: Impossibile creare config nell'environment sbagliato
- **Automazione**: Environment detection automatico senza intervento manuale
- **Consistency**: API v2 consistency migliorata
- **Maintainability**: Codice più pulito senza parametri ridondanti

### ✅ Sprint 3 - COMPLETATO (Agosto 2024)
**Status**: **COMPLETED** - Automazione Environment per Configuration Management

#### Obiettivi Raggiunti
1. ✅ **Eliminazione IsDev Manuale**: Rimosso parametro IsDev da tutti i DTO pubblici
2. ✅ **Automazione Environment**: `IHostEnvironment.IsDevelopment()` per detection automatico
3. ✅ **Fluxor State Management**: State management aggiornato per rimuovere gestione IsDev  
4. ✅ **Config.razor UI**: Interfaccia utente semplificata senza campo IsDev
5. ✅ **Special Character Support**: Supporto completo per caratteri speciali sicuri nelle configurazioni
6. ✅ **End-to-End Testing**: Validazione completa dev/prod environments e character handling

#### Risultati Architetturali
- **Zero Configuration Error Risk**: Impossibile creare config nell'environment sbagliato
- **Enhanced User Experience**: UI semplificata senza parametri tecnici
- **Improved API Consistency**: Contract API v2 pulito e coerente
- **Special Character Robustness**: Gestione sicura di caratteri speciali come `?@#%&*+=[]()` nelle configurazioni

### Note Tecniche
#### Configurazioni Environment
- **Development**: Utilizza `IHostEnvironment.IsDevelopment() = true`
- **Production**: Utilizza `IHostEnvironment.IsDevelopment() = false`
- **Database**: Campo IsDev mantenuto per compatibilità e query performance
- **API Responses**: IsDev non più incluso (filtering automatico lato server)

#### Compatibilità
- **Legacy Code**: ConfigsDto mantiene IsDev per backward compatibility UI
- **Modern API**: Shared DTOs (ConfigRequest/Response/Update) senza IsDev
- **Database Schema**: Nessuna modifica richiesta al database esistente