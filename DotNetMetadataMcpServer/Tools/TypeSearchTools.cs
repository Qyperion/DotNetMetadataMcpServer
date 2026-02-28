using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class TypeSearchTools
{
    [McpServerTool(Name = "TypeSearch")]
    [Description("Searches for types across project and dependency assemblies by type name.")]
    public static string SearchTypes(
        TypeSearchToolService typeSearchToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<TypeSearchTools> logger,
        [Description("The absolute path to the project file (.csproj)")] string projectFileAbsolutePath,
        [Description("Search query for full type name or short type name (e.g., 'Controller', 'JsonSerializer')")] string searchQuery,
        [Description("The assembly names to filter by (without exe/dll extension). If empty, all assemblies are considered")] List<string>? assemblyNames = null,
        [Description("Full text filters with wildcard support (e.g., '*Controller', 'System.*')")] List<string>? fullTextFiltersWithWildCardSupport = null,
        [Description("Sort field: 'fullName' (default) or 'assemblyName'")] string sortBy = "fullName",
        [Description("Sort direction: 'asc' (default) or 'desc'")] string sortDirection = "asc",
        [Description("Page number (1-based)")] int pageNumber = 1)
    {
        using var _ = logger.BeginScope("{TypeSearchToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation(
            "Received request to search types for project: {ProjectPath}, Query: {Query}, Page: {PageNumber}",
            projectFileAbsolutePath,
            searchQuery,
            pageNumber);

        try
        {
            var result = typeSearchToolService.SearchTypes(
                projectFileAbsolutePath: projectFileAbsolutePath,
                searchQuery: searchQuery,
                allowedAssemblyNames: assemblyNames ?? [],
                filters: fullTextFiltersWithWildCardSupport ?? [],
                sortBy: sortBy,
                sortDirection: sortDirection,
                pageNumber: pageNumber,
                pageSize: toolsConfiguration.Value.DefaultPageSize);

            logger.LogDebug("Type search completed successfully: {@TypeSearchResult}", result);

            return ToolResultHelper.Serialize(result, toolsConfiguration.Value.IndentResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching types");
            return ToolResultHelper.SerializeError(ex, toolsConfiguration.Value.IndentResponse, "TypeSearch");
        }
    }
}
