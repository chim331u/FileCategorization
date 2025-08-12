# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build and Run
```bash
dotnet build                    # Build the project
dotnet run                      # Run in development mode (launches Swagger at http://localhost:5089)
dotnet run --launch-profile https  # Run with HTTPS (https://localhost:7128)
```

### Testing
```bash
dotnet test                     # Run all unit tests
dotnet test --verbosity normal  # Run tests with detailed output
dotnet test --filter "ClassName" # Run tests for specific class
```

Tests are located in the `Tests/` folder and use xUnit with in-memory database for repository testing.

### Database Operations
```bash
dotnet ef migrations add <MigrationName>  # Add new migration
dotnet ef database update                 # Apply migrations
```

The application automatically applies migrations on startup.

## Project Architecture

This is a .NET 8 Web API project implementing a file categorization system with machine learning capabilities.

### Core Architecture Pattern
- **Minimal API Endpoints**: Uses endpoint mapping pattern instead of controllers
- **Dependency Injection**: Services registered in `Common/ServiceExtensions.cs`
- **Repository Pattern**: All modern endpoints use Repository Pattern with Result Pattern
- **Clean Architecture**: Separation of concerns following repository pattern best practices
- **Entity Framework**: SQLite database with Code First approach
- **Result Pattern**: Structured error handling using `Result<T>` wrapper for all operations

### Key Components

#### Domain Layer (`Domain/`)
- **Entities/**: All domain models, DTOs, and entity classes
  - **FilesDetail**: Core entity for file metadata and categorization status
  - **Configs**: Application configuration settings
  - **DD_LinkEd2k/DD_Threads**: DownloadDaemon integration models
  - **ApplicationUser**: Identity management with JWT authentication
  - **Identity DTOs**: LoginModelDto, SignupModelDto, TokenModelDto, Roles
- **Enums/**: All enumeration types (FileFilterType, MoveFilesResults)

#### Service Layer (`Services/`)
- **IFilesDetailService**: Legacy file management and categorization operations
- **IFilesQueryService**: Modern file querying with Repository Pattern
- **IConfigQueryService**: Modern configuration management with Repository Pattern  
- **IMachineLearningService**: ML-based file categorization using ML.NET
- **IDDService**: DownloadDaemon integration for link processing
- **IHangFireJobService**: Background job processing
- **IIdentityService**: User authentication and authorization
- **IUtilityServices**: Encryption, hashing, and utility operations

#### Repository Layer (`Infrastructure/`)
- **IFilesDetailRepository**: File data access with Repository Pattern
- **IConfigRepository**: Configuration data access with Repository Pattern
- **IUtilityRepository**: Utility operations repository
- **IRepository<T>**: Generic repository interface with common CRUD operations

#### External Integrations
- **Hangfire**: Background job processing with in-memory storage
- **SignalR**: Real-time notifications via `/notifications` hub
- **Serilog**: Structured logging to console and file

### API Structure (`Endpoints/`)

All endpoints are now consolidated in the `Endpoints/` folder following modern architecture patterns:

#### Legacy v1 Endpoints (`/api/v1/`)
- **Files Detail endpoints**: File CRUD operations and categorization (marked obsolete)
- **Configs endpoints**: Application configuration management (marked obsolete)  
- **Actions endpoints**: File movement and processing actions
- **DD endpoints**: DownloadDaemon integration
- **Utility endpoints**: Various utility functions (marked obsolete)
- **Identity endpoints**: User authentication

#### Modern v2 Endpoints (`/api/v2/`)
- **Files Query v2**: File querying with Repository Pattern and Result Pattern
- **Files Management v2**: File management operations with modern architecture
- **Configuration v2**: Configuration management with Repository Pattern, FluentValidation, and AutoMapper
- **Utilities v2**: Utility services with Repository Pattern (encrypt/decrypt, hash/verify operations)

All v2 endpoints implement:
- Repository Pattern for data access
- Result Pattern for structured error handling
- FluentValidation for input validation with ValidationFilter<T>
- AutoMapper for DTO/Entity mapping
- Comprehensive error handling and logging

### Authentication
- JWT Bearer token authentication
- Token expiration configured via `TokenExpirationMinutes` setting
- Development mode uses `JWT:Secret` from appsettings
- Production uses `JWT_SECRET` environment variable

### Database
- SQLite database stored in `/data/FileCat.db` (production) or `Temp/FileCat.db` (development)
- Migrations in `Migrations/` folder
- Entity Framework context: `ApplicationContext`

### Key Features
- File categorization using machine learning
- Background processing for file operations
- Real-time notifications
- DownloadDaemon integration for ed2k links
- JWT-based authentication
- Comprehensive API documentation via Swagger

### Development Environment Details

#### Key NuGet Packages
- **ML.NET (4.0.2)**: Machine learning framework for file categorization
- **Hangfire (1.8.18)**: Background job processing with in-memory storage
- **Entity Framework Core (8.0.15)**: ORM with SQLite provider
- **FluentValidation (11.11.0)**: Request validation for v2 endpoints
- **AutoMapper (12.0.1)**: DTO/Entity mapping for v2 endpoints
- **Serilog**: Structured logging with file and console outputs
- **SignalR**: Real-time web functionality
- **HtmlAgilityPack (1.12.1)**: HTML parsing capabilities
- **SSH.NET (2024.2.0)**: SSH operations support
- **xUnit (2.9.3)**: Testing framework with Moq for mocking

#### Configuration Differences
- **Development**: Database stored in `Temp/FileCat.db`, logs in `Temp/FC/`, JWT secret from appsettings
- **Production**: Database in `/data/FileCat.db`, logs in `/data/Log/`, JWT secret from environment variable `JWT_SECRET`

#### Service Registration Pattern
Services are registered in `Common/ServiceExtensions.cs`:
- **Legacy services**: Interface/implementation pattern registered as Scoped
- **Repository services**: Repository Pattern implementations registered as Scoped
- **Query services**: Application services with business logic registered as Scoped
- **Validators**: FluentValidation validators registered via `AddValidatorsFromAssembly`
- **AutoMapper**: Profiles registered for DTO/Entity mapping
- **ILogger**: Generic logger registered as Singleton for endpoint injection
- **IHostEnvironment**: Environment detection for configuration filtering

#### Endpoint Architecture
All endpoints are consolidated in the `Endpoints/` folder:
- **Minimal API pattern**: Extension methods for clean endpoint registration
- **Modern architecture features**:
  - Repository Pattern for data access
  - Result Pattern for error handling
  - ValidationFilter<T> for automatic request validation
  - AutoMapper for DTO conversion
  - Comprehensive Swagger documentation with versioning
  - Environment-aware configuration filtering

#### Modern Architecture Features
- **Result Pattern**: Structured error handling with `Result<T>` wrapper and `FromException` method
- **Repository Pattern**: Generic `IRepository<T>` with specialized implementations
- **Clean Architecture**: Organized by responsibility rather than technical layers
- **Validation Filters**: Automatic request validation using `ValidationFilter<T>`
- **DTO Mapping**: Automatic entity/DTO conversion using AutoMapper profiles
- **Environment Filtering**: Configuration filtering based on `IsDev` property
- **Comprehensive Testing**: Unit tests with xUnit, Moq, and in-memory database

## Development Guidelines

### API Versioning Strategy
- **v1 endpoints**: Legacy endpoints marked as obsolete but maintained for backward compatibility
- **v2 endpoints**: Modern architecture using Repository Pattern, Result Pattern, and Clean Architecture
- **Migration path**: v1 endpoints include deprecation notices pointing to v2 equivalents

### Code Organization
```
FileCategorization_Api/
├── Common/                     # Shared components and utilities
│   ├── ServiceExtensions.cs   # Dependency injection configuration
│   ├── ValidationFilter.cs    # Generic validation filter for endpoints
│   ├── Result.cs              # Result pattern implementation
│   └── [Mappings, Validators, Exceptions, Configurations]
│
├── Domain/                     # Domain layer
│   ├── Entities/              # All models, DTOs, and entity classes
│   │   ├── FileCategorization/ # Core domain entities
│   │   ├── Identity/          # Authentication DTOs and models
│   │   ├── DD/                # DownloadDaemon integration DTOs
│   │   └── [Other domain areas]
│   └── Enums/                 # Enumeration types
│
├── Endpoints/                  # API endpoint implementations
│   ├── [Legacy v1 endpoints]
│   └── [Modern v2 endpoints]
│
├── Infrastructure/             # Data access layer
│   └── Data/                  # Entity Framework and repositories
│       ├── ApplicationContext.cs
│       └── Repositories/      # Repository implementations
│
├── Interfaces/                 # All service and repository interfaces
│   ├── IRepository.cs         # Generic repository interface
│   └── [Specific interfaces]
│
├── Services/                   # Business logic services
│   ├── [Legacy services]
│   └── [Modern services]
│
├── Tests/                      # Unit tests
│   └── ConfigRepositoryTests.cs # xUnit tests with in-memory database
│
└── [Migrations, Properties, wwwroot]
```

### Best Practices
- **Error Handling**: Always use Result Pattern with `Result<T>.Success()`, `Result<T>.Failure()`, and `Result<T>.FromException()`
- **Validation**: Use FluentValidation with `ValidationFilter<T>` for automatic request validation
- **Logging**: Inject ILogger and log operations, errors, and performance metrics
- **Testing**: Write unit tests for repositories using in-memory database with xUnit and Moq
- **Documentation**: Include comprehensive XML comments and Swagger metadata
- **Environment Filtering**: Use `IHostEnvironment.IsDevelopment()` for environment-aware configuration

## Testing Strategy
- **Unit Tests**: Located in `Tests/` folder using xUnit framework
- **Repository Testing**: Uses in-memory Entity Framework database for isolated testing
- **Mocking**: Moq library for mocking dependencies like ILogger and IHostEnvironment
- **Test Naming**: Tests follow pattern `{MethodName}_{Scenario}_{ExpectedResult}`
- **Environment Mocking**: Use `EnvironmentName` property instead of extension methods for Moq compatibility

## Recent Architecture Improvements (2024)

### Repository Pattern Reorganization
The project structure has been completely reorganized following modern repository pattern best practices:

- **Consolidated Structure**: All endpoints moved to `Endpoints/`, all interfaces to `Interfaces/`, all services to `Services/`
- **Domain-Driven Organization**: Domain models and DTOs organized in `Domain/Entities/` by business area
- **Clean Separation**: Clear separation between domain, infrastructure, and application layers
- **Namespace Consistency**: All namespaces updated to reflect new structure (`FileCategorization_Api.Domain.Entities.*`)
- **Result Pattern Enhancement**: Improved error handling with `Result<T>.FromException()` method
- **Environment-Aware Config**: Configuration filtering based on development/production environment
- **Testing Improvements**: Enhanced test setup with proper mocking for environment-dependent code

### Benefits of New Structure
- **Improved Maintainability**: Easier to locate and modify related code
- **Better Testability**: Clean separation enables better unit testing
- **Scalability**: Structure supports future growth and new features
- **Developer Experience**: Consistent patterns and clear organization
- **Performance**: Optimized service registration and dependency injection