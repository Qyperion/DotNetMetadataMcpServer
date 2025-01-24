using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ModelContextProtocol.NET.Core.Models.Protocol.Client.Responses;
using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content;
using ModelContextProtocol.NET.Server.Contexts;
using ModelContextProtocol.NET.Server.Features.Tools;
using ModelContextProtocol.NET.Server.Session;

namespace DotNetMetadataMcpServer;

public class MyMetadataToolHandler(
    IServerContext serverContext,
    ISessionFacade sessionFacade,
    MyProjectScanner scanner,
    ILogger<MyMetadataToolHandler> logger
) : ToolHandlerBase<MetadataParameters>(tool, serverContext, sessionFacade)
{
    private static readonly Tool tool = new()
    {
        Name = "MetadataExplorer",
        Description = "Scans a .csproj for public reflection metadata and package references with recursive metadata information",
        InputSchema = MetadataParametersJsonContext.Default.MetadataParameters.GetToolSchema<MetadataParameters>()!
    };

    public override JsonTypeInfo JsonTypeInfo =>
        MetadataParametersJsonContext.Default.MetadataParameters;

    protected override Task<CallToolResult> HandleAsync(MetadataParameters parameters, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received request to scan project {Path}", parameters.ProjectFileAbsolutePath);

        ProjectMetadata result;
        try
        {
            result = scanner.ScanProject(parameters.ProjectFileAbsolutePath);
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Error scanning project {Path}", parameters.ProjectFileAbsolutePath);
            throw; // MCP сам сформирует ErrorResponse
        }
        
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        // Возвращаем через MCP
        var content = new TextContent { Text = json };
        var callToolResult = new CallToolResult { Content = new Annotated[] { content } };

        return Task.FromResult(callToolResult);
    }
}
