using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models.Base;
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
    public static async Task<string> GetDependencyGraph(
        DependencyGraphToolService dependencyGraphToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<DependencyGraphTools> logger,
        [Description("The absolute path to the project file (.csproj)")] string projectFileAbsolutePath,
        [Description("Include dependency name filters (wildcards). If empty, all are included.")] List<string>? includeFiltersWithWildCardSupport = null,
        [Description("Exclude dependency name filters (wildcards). Exclusions are applied after includes.")] List<string>? excludeFiltersWithWildCardSupport = null,
        [Description("Include dependency frameworks (wildcards).") ] List<string>? includeFrameworks = null,
        [Description("Exclude dependency frameworks (wildcards).") ] List<string>? excludeFrameworks = null,
        [Description("Maximum traversal depth. 0 means unlimited.")] int maxDepth = 0,
        [Description("Output mode: 'tree' (default) or 'flat'.")] string viewMode = DependencyGraphViewModes.Tree,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope("{DependencyGraphToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation("Received request to retrieve dependency graph for project: {ProjectPath}", projectFileAbsolutePath);

        try
        {
            var result = await ToolResultHelper.ExecuteWithTimeoutAsync(
                token => Task.Run(() => dependencyGraphToolService.GetDependencyGraph(
                    projectFileAbsolutePath,
                    includeFiltersWithWildCardSupport ?? [],
                    excludeFiltersWithWildCardSupport ?? [],
                    includeFrameworks ?? [],
                    excludeFrameworks ?? [],
                    maxDepth,
                    viewMode,
                    token), token),
                toolsConfiguration.Value.DependencyGraphToolTimeoutSeconds,
                cancellationToken);

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
