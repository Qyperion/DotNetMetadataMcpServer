

## Instructions for LLM how to work with the MCP server

It is recommended to put the instructions into some rule file to instruct the LLM how to work with the MCP server.

It is your instruction for the MCP server:
1. Retrieve all the assemblies by the project file
2. Determine assemblies in which you are interested
3. Retrieve namespaces from that assemblies
4. Retrieve types from namespaces that interested you
5. Use filers only if you overwhelmed by data amount or you are 100% sure which types you need


## Configuration

Configuration example:
```json
{
  "mcpServers": {
    "dotnet-types-explorer": {
      "command": "/home/vladimir/GitRoot/Experiments/DotNetMcpServer/DotNetMcpServer/bin/Release/net9.0/linux-x64/publish/DotNetMetadataMcpServer",
      "args": [ "--homeEnvVariable", "/home/vladimir" ],
      "disabled": false,
      "alwaysAllow": [],
      "timeout": 300
    }
  }
}
```