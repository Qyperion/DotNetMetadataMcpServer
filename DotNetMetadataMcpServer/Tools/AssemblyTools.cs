using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class AssemblyTools
{
    [McpServerTool(Name = "ReferencedAssembliesExplorer")]
    [Description("Retrieves referenced assemblies based on filters and pagination (doesn't extract data from referenced projects. Notice that the project must be built before scanning.")]
    public static async Task<string> GetReferencedAssemblies(
        AssemblyToolService assemblyToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<AssemblyTools> logger,
        [Description("The absolute path to the project file (.csproj)")] string projectFileAbsolutePath,
        [Description("Full text filters with wildcard support (e.g., 'System.*', '*Json*')")] List<string>? fullTextFiltersWithWildCardSupport = null,
        [Description("Page number (1-based)")] int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope("{AssemblyToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation("Received request to retrieve assemblies list for project: {ProjectPath}, Page: {PageNumber}",
            projectFileAbsolutePath, pageNumber);

        try
        {
            var filters = fullTextFiltersWithWildCardSupport ?? [];
            var result = await ToolResultHelper.ExecuteWithTimeoutAsync(
                token => Task.Run(() => assemblyToolService.GetAssemblies(
                    projectFileAbsolutePath: projectFileAbsolutePath,
                    filters: filters,
                    pageNumber: pageNumber,
                    pageSize: toolsConfiguration.Value.DefaultPageSize,
                    cancellationToken: token), token),
                toolsConfiguration.Value.AssemblyToolTimeoutSeconds,
                cancellationToken);

            logger.LogDebug("Project scanned successfully: {@AssembliesScanResult}", result);

            return ToolResultHelper.Serialize(result, toolsConfiguration.Value.IndentResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scanning project {Path}", projectFileAbsolutePath);
            return ToolResultHelper.SerializeError(ex, toolsConfiguration.Value.IndentResponse, "ReferencedAssembliesExplorer");
        }
    }
}

