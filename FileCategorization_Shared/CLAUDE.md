# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FileCategorization_Shared is a .NET 8.0 class library that serves as a shared component for the FileCategorization solution. This library contains common models, utilities, and data transfer objects that are shared between the API and Web projects.

## Architecture

This is a minimal shared library project with:
- Target Framework: .NET 8.0
- Nullable reference types enabled
- Implicit usings enabled
- No external dependencies (pure .NET library)

The project follows standard .NET library conventions and serves as a common dependency for other projects in the FileCategorization solution.

## Common Commands

### Build
```bash
dotnet build
```

### Clean
```bash
dotnet clean
```

### Restore packages
```bash
dotnet restore
```

### Build in Release mode
```bash
dotnet build -c Release
```

## Development Notes

- This is a shared library project without executable output
- No specific test framework is configured for this project
- The library is referenced by other projects in the solution
- All C# language features up to .NET 8.0 are available