using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class AssemblyTools
{
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    [McpServerTool(Name = "ReferencedAssembliesExplorer")]
    [Description("Retrieves referenced assemblies based on filters and pagination (doesn't extract data from referenced projects. Notice that the project must be built before scanning.")]
    public static string GetReferencedAssemblies(
        AssemblyToolService assemblyToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<AssemblyTools> logger,
        [Description("The absolute path to the project file (.csproj)")] string projectFileAbsolutePath,
        [Description("Full text filters with wildcard support (e.g., 'System.*', '*Json*')")] List<string>? fullTextFiltersWithWildCardSupport = null,
        [Description("Page number (1-based)")] int pageNumber = 1)
    {
        using var _ = logger.BeginScope("{AssemblyToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation("Received request to retrieve assemblies list for project: {ProjectPath}, Page: {PageNumber}",
            projectFileAbsolutePath, pageNumber);

        try
        {
            var filters = fullTextFiltersWithWildCardSupport ?? [];
            var result = assemblyToolService.GetAssemblies(
                projectFileAbsolutePath: projectFileAbsolutePath,
                filters: filters,
                pageNumber: pageNumber,
                pageSize: toolsConfiguration.Value.DefaultPageSize);

            logger.LogDebug("Project scanned successfully: {@AssembliesScanResult}", result);

            var json = toolsConfiguration.Value.IndentResponse
                ? JsonSerializer.Serialize(result, IndentedOptions)
                : JsonSerializer.Serialize(result);

            return json;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scanning project {Path}", projectFileAbsolutePath);
            throw;
        }
    }
}

