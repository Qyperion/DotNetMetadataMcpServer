using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DotNetMetadataMcpServer.Tools;

[McpServerToolType]
public sealed class NuGetTools
{
    [McpServerTool(Name = "NuGetPackageSearch")]
    [Description("Searches for NuGet packages on nuget.org with support for filtering and pagination.")]
    public static async Task<string> SearchPackages(
        NuGetToolService nuGetToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<NuGetTools> logger,
        [Description("The search query to find packages")] string searchQuery,
        [Description("Include prerelease versions in search results")] bool includePrerelease = false,
        [Description("Full text filters with wildcard support")] List<string>? fullTextFiltersWithWildCardSupport = null,
        [Description("Sort field: 'relevance' (default), 'id', 'version', 'downloads', 'published'")] string sortBy = "relevance",
        [Description("Sort direction: 'asc' (default) or 'desc'")] string sortDirection = "asc",
        [Description("Page number (1-based)")] int pageNumber = 1)
    {
        using var _ = logger.BeginScope("{NuGetSearchToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation("Received request to search NuGet packages: {Query}, IncludePrerelease: {IncludePrerelease}, Page: {PageNumber}",
            searchQuery, includePrerelease, pageNumber);

        try
        {
            var filters = fullTextFiltersWithWildCardSupport ?? [];

            var result = await nuGetToolService.SearchPackagesAsync(
                searchQuery: searchQuery,
                filters: filters,
                includePrerelease: includePrerelease,
                sortBy: sortBy,
                sortDirection: sortDirection,
                pageNumber: pageNumber,
                pageSize: toolsConfiguration.Value.DefaultPageSize);

            logger.LogDebug("NuGet packages search completed successfully: {@SearchResult}", result);

            return ToolResultHelper.Serialize(result, toolsConfiguration.Value.IndentResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching NuGet packages");
            return ToolResultHelper.SerializeError(ex, toolsConfiguration.Value.IndentResponse, "NuGetPackageSearch");
        }
    }

    [McpServerTool(Name = "NuGetPackageVersions")]
    [Description("Retrieves version history and dependency information for a specific NuGet package.")]
    public static async Task<string> GetPackageVersions(
        NuGetToolService nuGetToolService,
        IOptions<ToolsConfiguration> toolsConfiguration,
        ILogger<NuGetTools> logger,
        [Description("The package ID to get versions for")] string packageId,
        [Description("Include prerelease versions in results")] bool includePrerelease = false,
        [Description("Full text filters with wildcard support")] List<string>? fullTextFiltersWithWildCardSupport = null,
        [Description("Sort field: 'relevance' (default), 'version', 'downloads', 'published'")] string sortBy = "relevance",
        [Description("Sort direction: 'asc' (default) or 'desc'")] string sortDirection = "asc",
        [Description("Page number (1-based)")] int pageNumber = 1)
    {
        using var _ = logger.BeginScope("{NuGetVersionsToolExecutionUid}", Guid.NewGuid());

        logger.LogInformation("Received request to get versions for NuGet package: {PackageId}, IncludePrerelease: {IncludePrerelease}, Page: {PageNumber}",
            packageId, includePrerelease, pageNumber);

        try
        {
            var filters = fullTextFiltersWithWildCardSupport ?? [];

            var result = await nuGetToolService.GetPackageVersionsAsync(
                packageId: packageId,
                filters: filters,
                includePrerelease: includePrerelease,
                sortBy: sortBy,
                sortDirection: sortDirection,
                pageNumber: pageNumber,
                pageSize: toolsConfiguration.Value.DefaultPageSize);

            logger.LogDebug("NuGet package versions retrieved successfully: {@VersionsResult}", result);

            return ToolResultHelper.Serialize(result, toolsConfiguration.Value.IndentResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting NuGet package versions");
            return ToolResultHelper.SerializeError(ex, toolsConfiguration.Value.IndentResponse, "NuGetPackageVersions");
        }
    }
}

