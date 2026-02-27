using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class DependencyGraphTools
{
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    [McpServerTool(Name = "DependencyGraphExplorer")]
    [Description("Builds and returns dependency graph for the given .NET project from project.assets.json including transitive dependencies.")]
    public static string GetDependencyGraph(
        DependencyGraphToolService dependencyGraphToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<DependencyGraphTools> logger,
        [Description("The absolute path to the project file (.csproj)")] string projectFileAbsolutePath)
    {
        using var _ = logger.BeginScope("{DependencyGraphToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation("Received request to retrieve dependency graph for project: {ProjectPath}", projectFileAbsolutePath);

        try
        {
            var result = dependencyGraphToolService.GetDependencyGraph(projectFileAbsolutePath);

            logger.LogDebug("Dependency graph retrieved successfully: {@DependencyGraphResult}", result);

            return toolsConfiguration.Value.IndentResponse
                ? JsonSerializer.Serialize(result, IndentedOptions)
                : JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dependency graph");
            throw;
        }
    }
}
