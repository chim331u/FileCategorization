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
- **Repository Pattern**: Services implement interfaces for data access
- **Entity Framework**: SQLite database with Code First approach

### Key Components

#### Domain Models
- **FilesDetail**: Core entity for file metadata and categorization status
- **Configs**: Application configuration settings
- **DD_LinkEd2k/DD_Threads**: DownloadDaemon integration models
- **ApplicationUser**: Identity management with JWT authentication

#### Service Layer
- **IFilesDetailService**: File management and categorization operations
- **IMachineLearningService**: ML-based file categorization using ML.NET
- **IDDService**: DownloadDaemon integration for link processing
- **IHangFireJobService**: Background job processing
- **IIdentityService**: User authentication and authorization

#### External Integrations
- **Hangfire**: Background job processing with in-memory storage
- **SignalR**: Real-time notifications via `/notifications` hub
- **Serilog**: Structured logging to console and file

### API Structure
All endpoints are mapped under `/api/v1/` with the following groups:
- Files Detail endpoints: File CRUD operations and categorization
- Configs endpoints: Application configuration management
- Actions endpoints: File movement and processing actions
- DD endpoints: DownloadDaemon integration
- Utility endpoints: Various utility functions
- Identity endpoints: User authentication

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