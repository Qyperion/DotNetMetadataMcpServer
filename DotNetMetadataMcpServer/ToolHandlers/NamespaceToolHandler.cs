using System.Text.Json.Serialization.Metadata;
using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Services;
using ModelContextProtocol.NET.Server.Features.Tools;
using ModelContextProtocol.NET.Server.Session;
using Microsoft.Extensions.Options;
using ModelContextProtocol.NET.Core.Models.Protocol.Client.Responses;
using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content;
using ModelContextProtocol.NET.Server.Contexts;

namespace DotNetMetadataMcpServer.ToolHandlers;

public class NamespaceToolHandler(
    IServerContext serverContext,
    ISessionFacade sessionFacade,
    NamespaceToolService namespaceToolService,
    IOptions<ToolsConfiguration> toolsConfiguration,
    ILogger<NamespaceToolHandler> logger
) : ToolHandlerBase<NamespaceToolParameters>(tool, serverContext, sessionFacade)
{
    private readonly ToolsConfiguration _toolsConfiguration = toolsConfiguration.Value;

    private static readonly Tool tool = new()
    {
        Name = "NamespacesExplorer",
        Description = "Retrieves namespaces from specified assemblies supporting filters and pagination (doesn't extract data from referenced projects. " +
                      "Notice that the project must be built before scanning.",
        InputSchema = NamespaceToolParametersJsonContext.Default.NamespaceToolParameters.GetToolSchema<NamespaceToolParameters>()!
    };

    protected override Task<CallToolResult> HandleAsync(NamespaceToolParameters parameters, CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope("{NamespaceToolExecutionUid}", Guid.NewGuid());
        
        logger.LogInformation("Received request to retrieve namespaces with {@Parameters}", parameters);

        NamespaceToolResponse result;
        try
        {
            result = namespaceToolService.GetNamespaces(
                projectFileAbsolutePath: parameters.ProjectFileAbsolutePath,
                allowedAssemblyNames: parameters.AssemblyNames.ToList(),
                filters: parameters.FullTextFiltersWithWildCardSupport,
                pageNumber: parameters.PageNumber,
                pageSize: _toolsConfiguration.DefaultPageSize);
            

            logger.LogDebug("Namespaces retrieved successfully: {@NamespacesScanResult}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving namespaces");
            throw;
        }

        var json = _toolsConfiguration.IntendResponse
            ? System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
            : System.Text.Json.JsonSerializer.Serialize(result);

        var content = new TextContent { Text = json };
        var callToolResult = new CallToolResult { Content = [content] };
        return Task.FromResult(callToolResult);
    }

    public override JsonTypeInfo JsonTypeInfo => NamespaceToolParametersJsonContext.Default.NamespaceToolParameters;
}