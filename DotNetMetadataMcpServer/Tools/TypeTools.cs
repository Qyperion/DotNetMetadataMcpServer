using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class TypeTools
{
    [McpServerTool(Name = "NamespaceTypes")]
    [Description("Retrieves types from specified namespaces supporting filters and pagination.")]
    public static string GetTypes(
        TypeToolService typeToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<TypeTools> logger,
        [Description("The absolute path to the project file (.csproj)")] string projectFileAbsolutePath,
        [Description("The namespaces to filter by. If empty, all namespaces are considered")] List<string>? namespaces = null,
        [Description("Full text filters with wildcard support (e.g., '*Controller', 'Service*')")] List<string>? fullTextFiltersWithWildCardSupport = null,
        [Description("Page number (1-based)")] int pageNumber = 1)
    {
        using var _ = logger.BeginScope("{TypeToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation("Received request to retrieve types for project: {ProjectPath}, Page: {PageNumber}",
            projectFileAbsolutePath, pageNumber);

        try
        {
            var filters = fullTextFiltersWithWildCardSupport ?? [];
            var allowedNamespaces = namespaces ?? [];

            var result = typeToolService.GetTypes(
                projectFileAbsolutePath: projectFileAbsolutePath,
                allowedNamespaces: allowedNamespaces,
                filters: filters,
                pageNumber: pageNumber,
                pageSize: toolsConfiguration.Value.DefaultPageSize);

            logger.LogDebug("Types retrieved successfully: {@TypesScanResult}", result);

            return ToolResultHelper.Serialize(result, toolsConfiguration.Value.IndentResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving types");
            return ToolResultHelper.SerializeError(ex, toolsConfiguration.Value.IndentResponse, "NamespaceTypes");
        }
    }
}

