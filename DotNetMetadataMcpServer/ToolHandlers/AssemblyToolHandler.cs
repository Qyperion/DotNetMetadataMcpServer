using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Services;
using ModelContextProtocol.NET.Server.Features.Tools;
using ModelContextProtocol.NET.Server.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.NET.Core.Models.Protocol.Client.Responses;
using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content;
using ModelContextProtocol.NET.Server.Contexts;

namespace DotNetMetadataMcpServer.ToolHandlers
{
    public class AssemblyToolHandler(
        IServerContext serverContext,
        ISessionFacade sessionFacade,
        IOptions<ToolsConfiguration> toolsConfiguration,
        AssemblyToolService assemblyToolService,
        ILogger<AssemblyToolHandler> logger
    ) : ToolHandlerBase<AssemblyToolParameters>(tool, serverContext, sessionFacade)
    {
        private readonly ToolsConfiguration _toolsConfiguration = toolsConfiguration.Value;

        private static readonly Tool tool = new()
        {
            Name = "ReferencedAssembliesExplorer",
            Description = "Retrieves referenced assemblies based on filters and pagination (doesn't extract data from referenced projects. " +
                          "Notice that the project must be built before scanning.",
            InputSchema = AssemblyToolParametersJsonContext.Default.AssemblyToolParameters.GetToolSchema<AssemblyToolParameters>()!,
        };

        protected override Task<CallToolResult> HandleAsync(AssemblyToolParameters parameters, CancellationToken cancellationToken = default)
        {
            using var _ = logger.BeginScope("{AssemblyToolExecutionUid}", Guid.NewGuid());   
            
            logger.LogInformation("Received request to retrieve assemblies list with {@Parameters}", parameters);
            
            AssemblyToolResponse result;
            try
            {
                result = assemblyToolService.GetAssemblies(
                    projectFileAbsolutePath: parameters.ProjectFileAbsolutePath, 
                    filters: parameters.FullTextFiltersWithWildCardSupport, 
                    pageNumber: parameters.PageNumber, 
                    pageSize: _toolsConfiguration.DefaultPageSize);
                
                
                logger.LogDebug("Project scanned successfully: {@AssembliesScanResult}", result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error scanning project {Path}", parameters.ProjectFileAbsolutePath);
                throw; // MCP will generate ErrorResponse itself
            }
            
            var json = _toolsConfiguration.IntendResponse 
                ? JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }) 
                : JsonSerializer.Serialize(result);
            
            var content = new TextContent { Text = json };
            var callToolResult = new CallToolResult { Content = [content] };
            
            return Task.FromResult(callToolResult);
        }

        public override JsonTypeInfo JsonTypeInfo => AssemblyToolParametersJsonContext.Default.AssemblyToolParameters;
    }
}