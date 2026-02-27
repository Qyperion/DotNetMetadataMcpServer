using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class InheritanceTools
{
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    [McpServerTool(Name = "InheritanceHierarchy")]
    [Description("Retrieves base type chain and derived types for a specific type across project and dependency assemblies.")]
    public static string GetInheritanceHierarchy(
        InheritanceToolService inheritanceToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<InheritanceTools> logger,
        [Description("The absolute path to the project file (.csproj)")] string projectFileAbsolutePath,
        [Description("Type full name or short name to inspect (e.g., 'Namespace.MyType' or 'MyType')")] string typeName)
    {
        using var _ = logger.BeginScope("{InheritanceToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation(
            "Received request to retrieve inheritance hierarchy for project: {ProjectPath}, Type: {TypeName}",
            projectFileAbsolutePath,
            typeName);

        try
        {
            var result = inheritanceToolService.GetHierarchy(projectFileAbsolutePath, typeName);

            logger.LogDebug("Inheritance hierarchy retrieved successfully: {@InheritanceResult}", result);

            return toolsConfiguration.Value.IndentResponse
                ? JsonSerializer.Serialize(result, IndentedOptions)
                : JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving inheritance hierarchy");
            throw;
        }
    }
}
