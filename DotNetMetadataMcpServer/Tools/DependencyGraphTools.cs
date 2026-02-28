using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class DependencyGraphTools
{
    [McpServerTool(Name = "DependencyGraphExplorer")]
    [Description("Builds and returns dependency graph for the given .NET project from project.assets.json including transitive dependencies.")]
    public static string GetDependencyGraph(
        DependencyGraphToolService dependencyGraphToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<DependencyGraphTools> logger,
        [Description("The absolute path to the project file (.csproj)")] string projectFileAbsolutePath,
        [Description("Include dependency name filters (wildcards). If empty, all are included.")] List<string>? includeFiltersWithWildCardSupport = null,
        [Description("Exclude dependency name filters (wildcards). Exclusions are applied after includes.")] List<string>? excludeFiltersWithWildCardSupport = null,
        [Description("Maximum traversal depth. 0 means unlimited.")] int maxDepth = 0,
        [Description("Output mode: 'tree' (default) or 'flat'.")] string viewMode = "tree")
    {
        using var _ = logger.BeginScope("{DependencyGraphToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation("Received request to retrieve dependency graph for project: {ProjectPath}", projectFileAbsolutePath);

        try
        {
            var result = dependencyGraphToolService.GetDependencyGraph(
                projectFileAbsolutePath,
                includeFiltersWithWildCardSupport ?? [],
                excludeFiltersWithWildCardSupport ?? [],
                maxDepth,
                viewMode);

            logger.LogDebug("Dependency graph retrieved successfully: {@DependencyGraphResult}", result);

            return ToolResultHelper.Serialize(result, toolsConfiguration.Value.IndentResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dependency graph");
            return ToolResultHelper.SerializeError(ex, toolsConfiguration.Value.IndentResponse, "DependencyGraphExplorer");
        }
    }
}
