using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.NET.Core.Models.Protocol.Client.Responses;
using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content;
using ModelContextProtocol.NET.Server.Contexts;
using ModelContextProtocol.NET.Server.Features.Tools;
using ModelContextProtocol.NET.Server.Session;

using MySolution.ProjectScanner.Core;
using MySolution.ProjectScanner.Models;

namespace MySolution.McpServer.Handlers;

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
        Description = "Scans a .csproj to retrieve reflection-based metadata from the main assembly + package dependencies.",
        InputSchema = MetadataParametersJsonContext.Default.MetadataParameters.GetToolSchema()!
    };

    public override JsonTypeInfo JsonTypeInfo =>
        MetadataParametersJsonContext.Default.MetadataParameters;

    protected override Task<CallToolResult> HandleAsync(MetadataParameters parameters, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received request to scan project {Path}", parameters.ProjectFilePath);

        ProjectMetadata result;
        try
        {
            // Запускаем сканирование
            result = scanner.ScanProject(parameters.ProjectFilePath);
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Error scanning project {Path}", parameters.ProjectFilePath);
            // MCP-сервер сам сформирует ErrorResponse.
            throw;
        }

        // Превращаем result в JSON
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        // Возвращаем клиенту MCP через TextContent
        var content = new TextContent { Text = json };
        var callToolResult = new CallToolResult { Content = new Annotated[] { content } };

        return Task.FromResult(callToolResult);
    }
}
