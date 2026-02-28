using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class InheritanceTools
{
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

            return ToolResultHelper.Serialize(result, toolsConfiguration.Value.IndentResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving inheritance hierarchy");
            return ToolResultHelper.SerializeError(ex, toolsConfiguration.Value.IndentResponse, "InheritanceHierarchy");
        }
    }
}
