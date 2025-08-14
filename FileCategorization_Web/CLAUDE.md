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