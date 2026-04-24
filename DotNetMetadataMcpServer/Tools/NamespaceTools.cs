using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class NamespaceTools
{
    [McpServerTool(Name = "NamespacesExplorer")]
    [Description("Retrieves namespaces from specified assemblies supporting filters and pagination (doesn't extract data from referenced projects. Notice that the project must be built before scanning.")]
    public static async Task<string> GetNamespaces(
        NamespaceToolService namespaceToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<NamespaceTools> logger,
        [Description("The absolute path to the project file (.csproj)")] string projectFileAbsolutePath,
        [Description("The assembly names to filter by (without exe/dll extension). If empty, all assemblies are considered")] List<string>? assemblyNames = null,
        [Description("Full text filters with wildcard support (e.g., 'System.*', '*Json*')")] List<string>? fullTextFiltersWithWildCardSupport = null,
        [Description("Page number (1-based)")] int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope("{NamespaceToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation("Received request to retrieve namespaces for project: {ProjectPath}, Page: {PageNumber}",
            projectFileAbsolutePath, pageNumber);

        try
        {
            var filters = fullTextFiltersWithWildCardSupport ?? [];
            var assemblies = assemblyNames ?? [];

            var result = await ToolResultHelper.ExecuteWithTimeoutAsync(
                token => Task.Run(() => namespaceToolService.GetNamespaces(
                    projectFileAbsolutePath: projectFileAbsolutePath,
                    allowedAssemblyNames: assemblies,
                    filters: filters,
                    pageNumber: pageNumber,
                    pageSize: toolsConfiguration.Value.DefaultPageSize,
                    cancellationToken: token), token),
                toolsConfiguration.Value.NamespaceToolTimeoutSeconds,
                cancellationToken);

            logger.LogDebug("Namespaces retrieved successfully: {@NamespacesScanResult}", result);

            return ToolResultHelper.Serialize(result, toolsConfiguration.Value.IndentResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving namespaces");
            return ToolResultHelper.SerializeError(ex, toolsConfiguration.Value.IndentResponse, "NamespacesExplorer");
        }
    }
}

