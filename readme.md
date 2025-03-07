# .NET Types Explorer MCP Server

A Model Context Protocol (MCP) server that provides detailed type information from .NET projects for AI coding agents.

## Overview

The .NET Types Explorer MCP Server is a powerful tool designed to help AI coding agents understand and work with .NET codebases. 
It provides a structured way to explore assemblies, namespaces, and types in .NET projects, making it easier for AI agents to generate accurate and context-aware code suggestions.

The server uses reflection to extract detailed type information from compiled .NET assemblies, including classes, interfaces, methods, properties, fields, and events. 
This information is then made available through a set of tools that can be used by AI agents to explore the codebase in a systematic way.

## Features

- **Assembly Exploration**: Retrieve a list of all assemblies referenced by a .NET project
- **Namespace Exploration**: Discover all namespaces within specified assemblies
- **Type Exploration**: Get detailed information about types within specified namespaces, including:
  - Full type names with generic parameters
  - Implemented interfaces
  - Constructors with parameters
  - Methods with return types and parameters
  - Properties with types and accessors
  - Fields with types and modifiers
  - Events with handler types
- **Filtering**: Apply wildcard filters to narrow down results
- **Pagination**: Handle large result sets with built-in pagination

## Prerequisites

- .NET 9.0 SDK or later
- A .NET project that you want to explore

## Installation

1. Clone the repository
2. Build the project:
   ```bash
   dotnet build -c Release
   ```
3. Publish the project:
   ```bash
   dotnet publish -c Release -r <runtime-identifier> --self-contained false
   ```
   Replace `<runtime-identifier>` with your target platform (e.g., `win-x64`, `linux-x64`, `osx-x64`).

## Configuration

To use the .NET Types Explorer MCP Server with an AI agent, you need to configure it in your MCP settings file. Here's an example configuration:

```json
{
  "mcpServers": {
    "dotnet-types-explorer": {
      "command": "/path/to/DotNetMetadataMcpServer",
      "args": [ "--homeEnvVariable", "/home/user" ],
      "disabled": false,
      "alwaysAllow": [],
      "timeout": 300
    }
  }
}
```

Replace `/path/to/DotNetMetadataMcpServer` with the actual path to the published executable, and `/home/user` with your home directory.

## Important Limitations

- **The project must be built before scanning.** The server relies on compiled assemblies to extract type information, so make sure to build your project before using the tools.
- **The tool doesn't follow references to other projects.** It only inspects the specified project and its NuGet dependencies. If you need to analyze multiple projects, you'll need to scan each one separately.
- **Performance may vary with large projects.** For very large codebases with many dependencies, consider using more specific filtering to improve performance.

## Usage

The server provides three main tools that can be used by AI agents:

1. **ReferencedAssembliesExplorer**: Retrieves referenced assemblies from a .NET project
2. **NamespacesExplorer**: Retrieves namespaces from specified assemblies
3. **NamespaceTypes**: Retrieves types from specified namespaces

### When to Use This Tool

AI coding assistants should use this MCP server in the following scenarios:

- When you need to inspect the API of specific third-party libraries or NuGet packages
- When you're uncertain about the available types, methods, or properties in a referenced library
- When you need to explore a .NET codebase systematically from assemblies to namespaces to types
- When you need detailed type information that isn't readily available in documentation

**Important Limitations:**
- The tool doesn't follow references to other projects - it only inspects the specified project and its NuGet dependencies
- Base Class Library (BCL) types are typically well-documented elsewhere, so focus on third-party and project-specific types
- The project must be built before scanning, as the tool relies on compiled assemblies

### Recommended Workflow for AI Assistants

AI assistants should follow this precise workflow when using the MCP server:

1. **Retrieve all assemblies** by the project file
   - Use the ReferencedAssembliesExplorer tool with the project file path
   - Focus on third-party libraries, not BCL assemblies (System.*, Microsoft.*)

2. **Identify relevant assemblies** for the current task
   - Select assemblies that are likely to contain the types you need
   - Skip well-known BCL assemblies unless specifically needed

3. **Retrieve namespaces** from those assemblies
   - Use the NamespacesExplorer tool with the project file path and selected assemblies
   - This provides a map of the library's organization

4. **Retrieve types** from namespaces that interest you
   - Use the NamespaceTypes tool with the project file path and selected namespaces
   - This gives you detailed type information including methods, properties, etc.

5. **Use filtering sparingly**
   - Only apply filters when you're overwhelmed by data or know exactly what you're looking for
   - Wildcard filters (e.g., "*Controller", "*Service") can help narrow down results

### Integrating with AI Assistant Rules

It's strongly recommended to include these instructions in your AI assistant's rules files to ensure consistent and effective use of the tool. Add the workflow and usage guidelines to:

- GitHub Copilot instructions
- VS Code AI assistant custom modes
- OpenAI Assistant instructions
- Claude or other AI assistant configuration

Example rule for AI assistants:
```
When working with .NET projects and you need to understand third-party library APIs:
1. Use the .NET Types Explorer MCP Server to systematically explore the codebase
2. Start with assemblies, then namespaces, then types - following the top-down approach
3. Focus on third-party libraries and project-specific types, not BCL
4. Remember the tool only inspects the specified project and its NuGet dependencies
5. The project must be built before scanning
```

## How It Works

The server uses the following process to extract type information:

1. **Project Evaluation**: Uses MSBuild to evaluate the project file and find the compiled assembly
2. **Assembly Loading**: Loads the compiled assembly and its dependencies
3. **Type Reflection**: Uses reflection to extract detailed information about types
4. **Filtering and Pagination**: Applies filters and pagination to the results
5. **Response Formatting**: Formats the results as JSON and returns them to the client

## API Reference

### ReferencedAssembliesExplorer

Retrieves referenced assemblies based on filters and pagination.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "ProjectFileAbsolutePath": {
      "type": "string"
    },
    "PageNumber": {
      "type": "integer"
    },
    "FullTextFiltersWithWildCardSupport": {
      "type": "array",
      "items": {
        "type": [
          "string",
          "null"
        ]
      }
    }
  },
  "required": [
    "ProjectFileAbsolutePath"
  ]
}
```

**Response:**
```json
{
  "AssemblyNames": ["Assembly1", "Assembly2", ...],
  "CurrentPage": 1,
  "AvailablePages": [1, 2, ...]
}
```

### NamespacesExplorer

Retrieves namespaces from specified assemblies supporting filters and pagination.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "ProjectFileAbsolutePath": {
      "type": "string"
    },
    "AssemblyNames": {
      "type": "array",
      "items": {
        "type": [
          "string",
          "null"
        ]
      }
    },
    "PageNumber": {
      "type": "integer"
    },
    "FullTextFiltersWithWildCardSupport": {
      "type": "array",
      "items": {
        "type": [
          "string",
          "null"
        ]
      }
    }
  },
  "required": [
    "ProjectFileAbsolutePath"
  ]
}
```

**Response:**
```json
{
  "Namespaces": ["Namespace1", "Namespace2", ...],
  "CurrentPage": 1,
  "AvailablePages": [1, 2, ...]
}
```

### NamespaceTypes

Retrieves types from specified namespaces supporting filters and pagination.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "ProjectFileAbsolutePath": {
      "type": "string"
    },
    "Namespaces": {
      "type": "array",
      "items": {
        "type": [
          "string",
          "null"
        ]
      }
    },
    "PageNumber": {
      "type": "integer"
    },
    "FullTextFiltersWithWildCardSupport": {
      "type": "array",
      "items": {
        "type": [
          "string",
          "null"
        ]
      }
    }
  },
  "required": [
    "ProjectFileAbsolutePath"
  ]
}
```

**Response:**
```json
{
  "TypeData": [
    {
      "FullName": "Namespace.TypeName",
      "Implements": ["Interface1", "Interface2", ...],
      "Constructors": ["(param1, param2)", ...],
      "Methods": ["ReturnType MethodName(param1, param2)", ...],
      "Properties": ["PropertyType PropertyName { get; set; }", ...],
      "Fields": ["FieldType FieldName", ...],
      "Events": ["event EventHandlerType EventName", ...]
    },
    ...
  ],
  "CurrentPage": 1,
  "AvailablePages": [1, 2, ...]
}
```

## Example Usage Scenario

Here's an example of how an AI agent might use the .NET Types Explorer MCP Server:

1. **Retrieve Assemblies**:
   ```json
   {
     "ProjectFileAbsolutePath": "/path/to/project.csproj"
   }
   ```

2. **Retrieve Namespaces** from a specific assembly:
   ```json
   {
     "ProjectFileAbsolutePath": "/path/to/project.csproj",
     "AssemblyNames": ["MyAssembly"]
   }
   ```

3. **Retrieve Types** from a specific namespace:
   ```json
   {
     "ProjectFileAbsolutePath": "/path/to/project.csproj",
     "Namespaces": ["MyAssembly.MyNamespace"]
   }
   ```

4. **Apply Filters** to find specific types:
   ```json
   {
     "ProjectFileAbsolutePath": "/path/to/project.csproj",
     "Namespaces": ["MyAssembly.MyNamespace"],
     "FullTextFiltersWithWildCardSupport": ["*Controller", "*Service"]
   }
   ```

## Roadmap

- Switch to a library-based approach for better integration
- Add NuGet integration to provide information about actual package versions
- Improve performance for large projects
- Add support for more detailed type information
- Enhance filtering capabilities
- Add support for method body analysis

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
