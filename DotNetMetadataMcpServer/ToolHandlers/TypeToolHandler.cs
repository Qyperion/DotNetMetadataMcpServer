using System.Text.Json.Serialization.Metadata;
using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Services;
using ModelContextProtocol.NET.Server.Features.Tools;
using ModelContextProtocol.NET.Server.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.NET.Core.Models.Protocol.Client.Responses;
using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content;
using ModelContextProtocol.NET.Server.Contexts;

namespace DotNetMetadataMcpServer.ToolHandlers;

public class TypeToolHandler(
    IServerContext serverContext,
    ISessionFacade sessionFacade,
    TypeToolService typeToolService, 
    IOptions<ToolsConfiguration> toolsConfiguration,
    ILogger<TypeToolHandler> logger
) : ToolHandlerBase<TypeToolParameters>(tool, serverContext, sessionFacade)
{
    private readonly ToolsConfiguration _toolsConfiguration = toolsConfiguration.Value;

    private static readonly Tool tool = new()
    {
        Name = "NamespaceTypes",
        Description = "Retrieves types from specified namespaces supporting filters and pagination.",
        InputSchema = TypeToolParametersJsonContext.Default.TypeToolParameters.GetToolSchema<TypeToolParameters>()!
    };

    protected override Task<CallToolResult> HandleAsync(TypeToolParameters parameters, CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope("{TypeToolExecutionUid}", Guid.NewGuid());
        logger.LogInformation("Received request to retrieve types with {@Parameters}", parameters);

        TypeToolResponse result;
        try
        {
            result = typeToolService.GetTypes(
                projectFileAbsolutePath: parameters.ProjectFileAbsolutePath,
                allowedNamespaces: parameters.Namespaces.ToList(),
                filters: parameters.FullTextFiltersWithWildCardSupport,
                pageNumber: parameters.PageNumber,
                pageSize: _toolsConfiguration.DefaultPageSize
            );

            logger.LogDebug("Types retrieved successfully: {@TypeToolResponse}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving types");
            throw;
        }
        
        var json = _toolsConfiguration.IntendResponse
            ? System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) 
            : System.Text.Json.JsonSerializer.Serialize(result);

        var content = new TextContent { Text = json };
        return Task.FromResult(new CallToolResult { Content = [content] });
    }

    public override JsonTypeInfo JsonTypeInfo => TypeToolParametersJsonContext.Default.TypeToolParameters;
}