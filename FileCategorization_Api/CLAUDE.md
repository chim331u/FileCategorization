# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build and Run
```bash
dotnet build                    # Build the project
dotnet run                      # Run in development mode (launches Swagger at http://localhost:5089)
dotnet run --launch-profile https  # Run with HTTPS (https://localhost:7128)
```

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
- **Dependency Injection**: Services registered in `Extensions/ServiceExtensions.cs`
- **Repository Pattern**: Modern v2 endpoints use Repository Pattern with Result Pattern
- **Clean Architecture**: Separation of concerns with Application, Core, Infrastructure, and Presentation layers
- **Entity Framework**: SQLite database with Code First approach

### Key Components

#### Domain Models
- **FilesDetail**: Core entity for file metadata and categorization status
- **Configs**: Application configuration settings
- **DD_LinkEd2k/DD_Threads**: DownloadDaemon integration models
- **ApplicationUser**: Identity management with JWT authentication

#### Service Layer
- **IFilesDetailService**: File management and categorization operations (v1)
- **IFilesQueryService** & **IFilesDetailRepository**: Modern v2 file management with Repository Pattern
- **IConfigQueryService** & **IConfigRepository**: Modern v2 configuration management with Repository Pattern
- **IUtilityRepository**: Modern v2 utility services with Repository Pattern
- **IMachineLearningService**: ML-based file categorization using ML.NET
- **IDDService**: DownloadDaemon integration for link processing
- **IHangFireJobService**: Background job processing
- **IIdentityService**: User authentication and authorization

#### External Integrations
- **Hangfire**: Background job processing with in-memory storage
- **SignalR**: Real-time notifications via `/notifications` hub
- **Serilog**: Structured logging to console and file

### API Structure

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
- FluentValidation for input validation
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
- **AutoMapper (13.0.2)**: DTO/Entity mapping for v2 endpoints
- **Serilog**: Structured logging with file and console outputs
- **SignalR**: Real-time web functionality
- **HtmlAgilityPack (1.12.1)**: HTML parsing capabilities
- **SSH.NET (2024.2.0)**: SSH operations support

#### Configuration Differences
- **Development**: Database stored in `Temp/FileCat.db`, logs in `Temp/FC/`, JWT secret from appsettings
- **Production**: Database in `/data/FileCat.db`, logs in `/data/Log/`, JWT secret from environment variable `JWT_SECRET`

#### Service Registration Pattern
Services are registered in `Extensions/ServiceExtensions.cs`:
- **Legacy services**: Interface/implementation pattern registered as Scoped
- **v2 Repository services**: Repository Pattern implementations registered as Scoped
- **v2 Query services**: Application services with business logic registered as Scoped
- **Validators**: FluentValidation validators registered via `AddValidatorsFromAssembly`
- **AutoMapper**: Profiles registered for DTO/Entity mapping
- **ILogger**: Generic logger registered as Singleton for endpoint injection

#### Endpoint Architecture
- **Legacy endpoints (`Endpoints/` folder)**: Minimal API pattern with extension methods
- **v2 endpoints (`Presentation/Endpoints/` folder)**: Modern architecture with:
  - Repository Pattern for data access
  - Result Pattern for error handling
  - Validation filters using FluentValidation
  - AutoMapper for DTO conversion
  - Comprehensive Swagger documentation with versioning

#### Modern Architecture Features (v2)
- **Result Pattern**: Structured error handling with `Result<T>` wrapper
- **Repository Pattern**: Generic repository with specialized implementations
- **Clean Architecture**: Organized in layers (Application, Core, Infrastructure, Presentation)
- **Validation Filters**: Automatic request validation using FluentValidation
- **DTO Mapping**: Automatic entity/DTO conversion using AutoMapper profiles
- **Comprehensive Testing**: Unit tests for repositories with in-memory database

## Development Guidelines

### API Versioning Strategy
- **v1 endpoints**: Legacy endpoints marked as obsolete but maintained for backward compatibility
- **v2 endpoints**: Modern architecture using Repository Pattern, Result Pattern, and Clean Architecture
- **Migration path**: v1 endpoints include deprecation notices pointing to v2 equivalents

### Code Organization
```
Application/
├── Mappings/           # AutoMapper profiles for DTO/Entity mapping
├── Services/           # Application services with business logic
└── Validators/         # FluentValidation validators

Core/
├── Common/            # Result Pattern and common utilities
└── Interfaces/        # Repository and service interfaces

Infrastructure/
├── Data/
│   └── Repositories/  # Repository Pattern implementations
└── Repositories/      # Utility repositories

Presentation/
├── Endpoints/         # Modern v2 endpoint implementations
└── Filters/          # Validation and other filters

Contracts/            # DTOs organized by domain
├── Config/
├── FilesDetail/
└── Utility/
```

### Best Practices
- **Error Handling**: Always use Result Pattern for v2 endpoints
- **Validation**: Use FluentValidation for all input validation
- **Logging**: Inject ILogger and log operations, errors, and performance metrics
- **Testing**: Write unit tests for repositories using in-memory database
- **Documentation**: Include comprehensive XML comments and Swagger metadata