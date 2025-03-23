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
    public class NuGetPackageSearchToolHandler(
        IServerContext serverContext,
        ISessionFacade sessionFacade,
        IOptions<ToolsConfiguration> toolsConfiguration,
        NuGetToolService nuGetToolService,
        ILogger<NuGetPackageSearchToolHandler> logger
    ) : ToolHandlerBase<NuGetPackageSearchParameters>(tool, serverContext, sessionFacade)
    {
        private readonly ToolsConfiguration _toolsConfiguration = toolsConfiguration.Value;

        private static readonly Tool tool = new()
        {
            Name = "NuGetPackageSearch",
            Description = "Searches for NuGet packages on nuget.org with support for filtering and pagination.",
            InputSchema = NuGetPackageSearchParametersJsonContext.Default.NuGetPackageSearchParameters.GetToolSchema<NuGetPackageSearchParameters>()!,
        };

        protected override async Task<CallToolResult> HandleAsync(NuGetPackageSearchParameters parameters, CancellationToken cancellationToken = default)
        {
            using var _ = logger.BeginScope("{NuGetPackageSearchExecutionUid}", Guid.NewGuid());   
            
            logger.LogInformation("Received request to search NuGet packages with {@Parameters}", parameters);
            
            NuGetPackageSearchResponse result;
            try
            {
                result = await nuGetToolService.SearchPackagesAsync(
                    searchQuery: parameters.SearchQuery, 
                    filters: parameters.FullTextFiltersWithWildCardSupport, 
                    includePrerelease: parameters.IncludePrerelease,
                    pageNumber: parameters.PageNumber, 
                    pageSize: _toolsConfiguration.DefaultPageSize);
                
                logger.LogDebug("NuGet packages search completed successfully: {@SearchResult}", result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching NuGet packages with query {Query}", parameters.SearchQuery);
                throw; // MCP will generate ErrorResponse itself
            }
            
            var json = _toolsConfiguration.IntendResponse 
                ? JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }) 
                : JsonSerializer.Serialize(result);
            
            var content = new TextContent { Text = json };
            var callToolResult = new CallToolResult { Content = [content] };
            
            return callToolResult;
        }

        public override JsonTypeInfo JsonTypeInfo => NuGetPackageSearchParametersJsonContext.Default.NuGetPackageSearchParameters;
    }
    
    public class NuGetPackageVersionsToolHandler(
        IServerContext serverContext,
        ISessionFacade sessionFacade,
        IOptions<ToolsConfiguration> toolsConfiguration,
        NuGetToolService nuGetToolService,
        ILogger<NuGetPackageVersionsToolHandler> logger
    ) : ToolHandlerBase<NuGetPackageVersionParameters>(tool, serverContext, sessionFacade)
    {
        private readonly ToolsConfiguration _toolsConfiguration = toolsConfiguration.Value;

        private static readonly Tool tool = new()
        {
            Name = "NuGetPackageVersions",
            Description = "Retrieves version history and dependency information for a specific NuGet package.",
            InputSchema = NuGetPackageVersionParametersJsonContext.Default.NuGetPackageVersionParameters.GetToolSchema<NuGetPackageVersionParameters>()!,
        };

        protected override async Task<CallToolResult> HandleAsync(NuGetPackageVersionParameters parameters, CancellationToken cancellationToken = default)
        {
            using var _ = logger.BeginScope("{NuGetPackageVersionsExecutionUid}", Guid.NewGuid());   
            
            logger.LogInformation("Received request to get NuGet package versions with {@Parameters}", parameters);
            
            NuGetPackageVersionsResponse result;
            try
            {
                result = await nuGetToolService.GetPackageVersionsAsync(
                    packageId: parameters.PackageId, 
                    filters: parameters.FullTextFiltersWithWildCardSupport, 
                    includePrerelease: parameters.IncludePrerelease,
                    pageNumber: parameters.PageNumber, 
                    pageSize: _toolsConfiguration.DefaultPageSize);
                
                logger.LogDebug("NuGet package versions retrieved successfully: {@VersionsResult}", result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting versions for NuGet package {PackageId}", parameters.PackageId);
                throw; // MCP will generate ErrorResponse itself
            }
            
            var json = _toolsConfiguration.IntendResponse 
                ? JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }) 
                : JsonSerializer.Serialize(result);
            
            var content = new TextContent { Text = json };
            var callToolResult = new CallToolResult { Content = [content] };
            
            return callToolResult;
        }

        public override JsonTypeInfo JsonTypeInfo => NuGetPackageVersionParametersJsonContext.Default.NuGetPackageVersionParameters;
    }
}