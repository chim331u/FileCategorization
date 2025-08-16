# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FileCategorization_Shared is a .NET 8.0 class library that serves as the **common foundation** for the FileCategorization solution. This library contains shared models, utilities, and data transfer objects that eliminate code duplication between the API and Web projects while ensuring consistent data contracts across the entire solution.

## Development Commands

### Build and Validation
```bash
# Build the shared library
dotnet build

# Build in Release mode for production
dotnet build --configuration Release

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore

# Pack for distribution (if needed)
dotnet pack --configuration Release
```

### Integration with Solution
```bash
# Build entire solution (from solution root)
dotnet build

# Add reference to this shared library from other projects
dotnet add reference ../FileCategorization_Shared/FileCategorization_Shared.csproj
```

## Architecture & Design Principles

### Core Design Philosophy
- **Zero Dependencies**: Pure .NET 8.0 library with no external packages
- **Cross-Platform Compatibility**: Works across all .NET 8.0 supported platforms  
- **Type Safety**: Full nullable reference types support for enhanced reliability
- **Immutable Patterns**: Favor immutable DTOs and readonly structures
- **Clear Boundaries**: Strict separation of concerns with well-defined interfaces

### Library Structure
```
FileCategorization_Shared/
├── Common/                 # Shared utilities and foundational types
│   ├── Result.cs          # Result pattern for structured error handling
│   └── ApiResponse.cs     # Standardized API response wrapper
├── DTOs/                  # Data Transfer Objects
│   ├── Configuration/     # Configuration-related DTOs
│   │   └── ConfigsDto.cs  # Application configuration DTO
│   └── FileManagement/    # File management DTOs
│       ├── FilesDetailDto.cs  # Core file metadata DTO
│       └── FileMoveDto.cs     # File movement operation DTO
├── Enums/                 # Shared enumeration types
│   └── MoveFilesResults.cs # File operation result enumeration
└── FileCategorization_Shared.csproj
```

## Current Shared Components

### **Common Utilities** (`Common/`)

#### **Result Pattern (`Result.cs`)**
- **Purpose**: Structured error handling across API and Web projects
- **Features**: 
  - Generic `Result<T>` with success/failure states
  - Exception handling with detailed context
  - Async-compatible with `MatchAsync` methods
  - Backward compatibility with `FromException` method
- **Usage**: All service layer operations should return `Result<T>` instead of throwing exceptions

#### **API Response Wrapper (`ApiResponse.cs`)**
- **Purpose**: Standardized response format for all API endpoints
- **Features**: Consistent response structure with data, success status, and error information

### **Data Transfer Objects** (`DTOs/`)

#### **Configuration DTOs** (`DTOs/Configuration/`)
- **`ConfigsDto`**: Application configuration settings shared between API and Web

#### **File Management DTOs** (`DTOs/FileManagement/`)
- **`FilesDetailDto`**: Core file metadata with categorization status
  - Properties: Id, Name, FileSize, FileCategory, IsToCategorize, IsNew, IsNotToMove
  - Used by: File listing, categorization, and management operations
- **`FileMoveDto`**: File movement operation parameters
  - Used by: File organization and batch movement operations

### **Shared Enumerations** (`Enums/`)
- **`MoveFilesResults`**: Standardized results for file operation outcomes
  - Used by: File movement operations, background job results, SignalR notifications

## Development Guidelines

### **When to Add to Shared Library**
✅ **Include in Shared:**
- DTOs used by both API and Web projects
- Common enums referenced across projects
- Shared response/request models
- Cross-cutting utilities (Result pattern, common exceptions)
- Domain models that represent core business entities

❌ **Keep in Individual Projects:**
- Project-specific services and repositories
- UI-specific models and view models
- Infrastructure-specific configurations
- Framework-specific extensions (SignalR hubs, Blazor components)

### **Namespace Conventions**
```csharp
// Shared DTOs
namespace FileCategorization_Shared.DTOs.{BusinessArea};

// Common utilities
namespace FileCategorization_Shared.Common;

// Shared enums
namespace FileCategorization_Shared.Enums;
```

### **Code Organization Best Practices**
- **Immutable DTOs**: Use `init` properties where possible for immutable data transfer objects
- **Nullable Context**: All properties should have explicit nullability annotations
- **XML Documentation**: Comprehensive documentation for all public members
- **Business Domain Organization**: Group DTOs by business area (Configuration, FileManagement, etc.)

### **Dependency Management**
- **Zero External Dependencies**: Keep the shared library dependency-free
- **.NET Standard Compatibility**: Use only .NET 8.0 BCL features
- **Forward Compatibility**: Design for future .NET versions

## Migration Strategy

### **Moving DTOs to Shared Library**
When consolidating DTOs from API or Web projects:

1. **Identify Shared Usage**: Confirm the DTO is used by multiple projects
2. **Create Shared Version**: Move to appropriate namespace in Shared project
3. **Update References**: Update using statements in API and Web projects
4. **Remove Duplicates**: Delete original DTO files from individual projects
5. **Update Project References**: Ensure both projects reference the Shared library

### **Example Migration Process**
```bash
# 1. Move DTO file to shared library
mv FileCategorization_Api/Domain/Entities/SomeDto.cs FileCategorization_Shared/DTOs/BusinessArea/

# 2. Update namespace in moved file
# Change: namespace FileCategorization_Api.Domain.Entities
# To: namespace FileCategorization_Shared.DTOs.BusinessArea

# 3. Update using statements in consuming projects
# Add: using FileCategorization_Shared.DTOs.BusinessArea;

# 4. Build solution to verify
dotnet build
```

## Quality Assurance

### **Design Validation**
- All shared models should be serializable (JSON/XML)
- DTOs should be anemic (data only, no business logic)
- Enums should have explicit values for versioning stability
- Common utilities should be pure functions where possible

### **Testing Strategy**
- Shared library components are tested through consuming projects
- Focus on integration testing in API and Web project test suites
- Validate serialization/deserialization of all DTOs
- Ensure enum value stability across versions

## Future Considerations

### **Planned Enhancements**
1. **DTO Consolidation**: Move remaining duplicate DTOs from API/Web to Shared
2. **Common Validation**: Shared validation attributes and rules
3. **Domain Events**: Shared event models for cross-project communication
4. **API Contracts**: OpenAPI schema generation from shared DTOs

### **Versioning Strategy**
- **Semantic Versioning**: Follow SemVer for breaking changes
- **Backward Compatibility**: Maintain compatibility within major versions
- **Deprecation Path**: Mark obsolete members before removal
- **Migration Guidance**: Document breaking changes and migration steps

## Integration with Solution Projects

### **FileCategorization_Api Integration**
- Uses shared DTOs for API responses and requests
- Implements Result pattern from shared library for error handling
- Maps entity models to shared DTOs using AutoMapper

### **FileCategorization_Web Integration**
- Consumes shared DTOs for API communication
- Uses Result pattern for service layer error handling
- Leverages shared enums for UI state management

### **Benefits of Shared Architecture**
- **Consistency**: Identical data contracts across API and Web
- **Maintainability**: Single source of truth for common models
- **Type Safety**: Compile-time validation of data contracts
- **Reduced Duplication**: Eliminates duplicate DTO definitions
- **Faster Development**: Reusable components across projects