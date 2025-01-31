using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ModelContextProtocol.NET.Core.Models.Protocol.Client.Responses;
using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content;
using ModelContextProtocol.NET.Server.Contexts;
using ModelContextProtocol.NET.Server.Features.Tools;
using ModelContextProtocol.NET.Server.Session;

namespace DotNetMetadataMcpServer;

public class DotNetProjectTypesExplorerToolHandler(
    IServerContext serverContext,
    ISessionFacade sessionFacade,
    DependenciesScanner scanner,
    ILogger<DotNetProjectTypesExplorerToolHandler> logger
) : ToolHandlerBase<MetadataParameters>(tool, serverContext, sessionFacade)
{
    private static readonly Tool tool = new()
    {
        Name = "DotNetProjectTypesExplorer",
        Description = "Scans a .csproj for available public types and members in the project and in all referenced NuGet packages " +
                      "(doesn't extract data from referenced projects for now). " +
                      "Notice that the project must be built before scanning.",
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scanning project {Path}", parameters.ProjectFileAbsolutePath);
            throw; // MCP will generate ErrorResponse itself
        }

        logger.LogInformation("Project {ProjectName} scanned successfully", result.ProjectName);
        
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        // Return through MCP
        var content = new TextContent { Text = json };
        var callToolResult = new CallToolResult { Content = new Annotated[] { content } };

        return Task.FromResult(callToolResult);
    }
}