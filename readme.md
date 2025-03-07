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

## Important Note

**The project must be built before scanning.** The server relies on compiled assemblies to extract type information, so make sure to build your project before using the tools.

## Usage

The server provides three main tools that can be used by AI agents:

1. **ReferencedAssembliesExplorer**: Retrieves referenced assemblies from a .NET project
2. **NamespacesExplorer**: Retrieves namespaces from specified assemblies
3. **NamespaceTypes**: Retrieves types from specified namespaces

### Recommended Workflow for AI Agents

It is recommended to follow this step-by-step approach when working with the MCP server:

1. Retrieve all the assemblies by the project file
2. Determine assemblies in which you are interested
3. Retrieve namespaces from those assemblies
4. Retrieve types from namespaces that interest you
5. Use filters only if you are overwhelmed by data amount or you are 100% sure which types you need

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
