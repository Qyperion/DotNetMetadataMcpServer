using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Models.Base;
using Microsoft.Extensions.Options;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace DotNetMetadataMcpServer.Services
{
    public class NuGetToolService
    {
        private readonly ILogger<NuGetToolService> _logger;
        private readonly List<SourceRepository> _repositories;
        private readonly NuGet.Common.ILogger _nugetLogger;
        private static readonly TimeSpan PerSourceTimeout = TimeSpan.FromSeconds(30);

        public NuGetToolService(ILogger<NuGetToolService> logger, IOptions<ToolsConfiguration> configuration)
        {
            _logger = logger;
            _nugetLogger = NullLogger.Instance;

            // Initialize repositories from configuration
            // Priority is determined by order in configuration (first = highest priority)
            _repositories = [];
            var sources = configuration.Value.NuGetSources.Where(s => s.Enabled).ToList();

            // Add default nuget.org ONLY if no sources configured
            if (!sources.Any())
            {
                _logger.LogWarning("No NuGet sources configured, adding default nuget.org");
                sources.Add(new NuGetSourceConfiguration
                {
                    Name = "nuget.org",
                    Url = "https://api.nuget.org/v3/index.json",
                    Enabled = true
                });
            }

            // Repositories are added in order - this order determines priority
            foreach (var source in sources)
            {
                try
                {
                    var repository = Repository.Factory.GetCoreV3(source.Url);
                    _repositories.Add(repository);
                    _logger.LogInformation("NuGet source added with priority {Priority}: {Name} ({Url})",
                        _repositories.Count, source.Name, source.Url);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add NuGet source: {Name} ({Url})", source.Name, source.Url);
                }
            }

            if (!_repositories.Any())
            {
                throw new InvalidOperationException("No valid NuGet sources configured");
            }
        }

        public async Task<NuGetPackageSearchResponse> SearchPackagesAsync(
            string searchQuery,
            List<string> filters,
            bool includePrerelease,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default,
            List<string>? includeFrameworks = null,
            List<string>? excludeFrameworks = null)
        {
            return await SearchPackagesAsync(
                searchQuery,
                filters,
                includePrerelease,
                NuGetSearchSortFields.Relevance,
                SortDirections.Asc,
                pageNumber,
                pageSize,
                cancellationToken,
                includeFrameworks ?? [],
                excludeFrameworks ?? []);
        }

        public async Task<NuGetPackageSearchResponse> SearchPackagesAsync(
            string searchQuery,
            List<string> filters,
            bool includePrerelease,
            string sortBy,
            string sortDirection,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default,
            List<string>? includeFrameworks = null,
            List<string>? excludeFrameworks = null)
        {
            _logger.LogInformation("Searching NuGet packages with query: {Query}, includePrerelease: {IncludePrerelease} across {SourceCount} sources",
                searchQuery, includePrerelease, _repositories.Count);

            try
            {
                var includeFrameworkPredicates = (includeFrameworks ?? [])
                    .Select(filter => FilteringHelper.PrepareFilteringPredicate(filter))
                    .ToList();
                var excludeFrameworkPredicates = (excludeFrameworks ?? [])
                    .Select(filter => FilteringHelper.PrepareFilteringPredicate(filter))
                    .ToList();

                // Search across all configured repositories in parallel for performance
                var searchTasks = _repositories.Select((repo, index) => new { Repo = repo, Priority = index })
                    .Select(async item =>
                    {
                        try
                        {
                            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            cts.CancelAfter(PerSourceTimeout);
                            var searchResource = await item.Repo.GetResourceAsync<PackageSearchResource>(cts.Token);
                            var results = await searchResource.SearchAsync(
                                searchQuery,
                                new SearchFilter(includePrerelease),
                                skip: 0,
                                take: 100,
                                _nugetLogger,
                                cts.Token);
                            return new { Results = results, Priority = item.Priority };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to search in repository at priority {Priority}", item.Priority);
                            return new { Results = Enumerable.Empty<IPackageSearchMetadata>(), Priority = item.Priority };
                        }
                    });

                var allResults = await Task.WhenAll(searchTasks);
                var packages = new Dictionary<string, NuGetPackageInfo>();

                // Process results in priority order (lower priority number = higher priority)
                foreach (var resultSet in allResults.OrderBy(r => r.Priority))
                {
                    foreach (var package in resultSet.Results)
                    {
                        // Priority-based deduplication: packages from higher priority sources (earlier in config)
                        // take precedence over packages from lower priority sources
                        if (!packages.ContainsKey(package.Identity.Id))
                        {
                            var frameworkNames = package.DependencySets
                                .Select(set => set.TargetFramework?.ToString())
                                .Where(f => !string.IsNullOrWhiteSpace(f))
                                .Select(f => f!)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            if (!MatchesFrameworkFilters(frameworkNames, includeFrameworkPredicates, excludeFrameworkPredicates))
                            {
                                continue;
                            }

                            packages[package.Identity.Id] = new NuGetPackageInfo
                            {
                                Id = package.Identity.Id,
                                Version = package.Identity.Version.ToString(),
                                Description = package.Description,
                                Authors = package.Authors,
                                DownloadCount = package.DownloadCount ?? 0,
                                Published = package.Published
                            };
                        }
                    }
                }

                var packageList = packages.Values.ToList();

                // Apply additional filtering if needed
                if (filters.Any())
                {
                    var predicates = filters.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter)).ToList();
                    packageList = packageList
                        .Where(p => predicates.Any(predicate =>
                            predicate.Invoke(p.Id) ||
                            (p.Description != null && predicate.Invoke(p.Description))))
                        .ToList();
                }

                packageList = OrderSearchResults(packageList, sortBy, sortDirection);

                // Apply pagination
                var (paged, availablePages) = PaginationHelper.FilterAndPaginate(
                    packageList,
                    _ => true,
                    pageNumber,
                    pageSize);

                return new NuGetPackageSearchResponse
                {
                    Packages = paged,
                    CurrentPage = pageNumber,
                    AvailablePages = availablePages,
                    TotalItems = packageList.Count,
                    PageSize = pageSize,
                    SortBy = NormalizeSearchSortBy(sortBy),
                    SortDirection = NormalizeSortDirection(sortDirection)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching NuGet packages with query: {Query}", searchQuery);
                throw;
            }
        }

        public async Task<NuGetPackageVersionsResponse> GetPackageVersionsAsync(
            string packageId,
            List<string> filters,
            bool includePrerelease,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default,
            List<string>? includeFrameworks = null,
            List<string>? excludeFrameworks = null)
        {
            return await GetPackageVersionsAsync(
                packageId,
                filters,
                includePrerelease,
                NuGetVersionSortFields.Relevance,
                SortDirections.Asc,
                pageNumber,
                pageSize,
                cancellationToken,
                includeFrameworks ?? [],
                excludeFrameworks ?? []);
        }

        public async Task<NuGetPackageVersionsResponse> GetPackageVersionsAsync(
            string packageId,
            List<string> filters,
            bool includePrerelease,
            string sortBy,
            string sortDirection,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default,
            List<string>? includeFrameworks = null,
            List<string>? excludeFrameworks = null)
        {
            _logger.LogInformation("Getting versions for NuGet package: {PackageId}, includePrerelease: {IncludePrerelease} across {SourceCount} sources",
                packageId, includePrerelease, _repositories.Count);

            try
            {
                var includeFrameworkPredicates = (includeFrameworks ?? [])
                    .Select(filter => FilteringHelper.PrepareFilteringPredicate(filter))
                    .ToList();
                var excludeFrameworkPredicates = (excludeFrameworks ?? [])
                    .Select(filter => FilteringHelper.PrepareFilteringPredicate(filter))
                    .ToList();

                // Query all repositories in parallel for performance
                var metadataTasks = _repositories.Select((repo, index) => new { Repo = repo, Priority = index })
                    .Select(async item =>
                    {
                        try
                        {
                            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            cts.CancelAfter(PerSourceTimeout);
                            var metadataResource = await item.Repo.GetResourceAsync<PackageMetadataResource>(cts.Token);
                            var results = await metadataResource.GetMetadataAsync(
                                packageId,
                                includePrerelease,
                                includeUnlisted: false,
                                new SourceCacheContext(),
                                _nugetLogger,
                                cts.Token);
                            return new { Results = results, Priority = item.Priority };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get metadata from repository at priority {Priority}", item.Priority);
                            return new { Results = Enumerable.Empty<IPackageSearchMetadata>(), Priority = item.Priority };
                        }
                    });

                var allMetadata = await Task.WhenAll(metadataTasks);
                var versionDict = new Dictionary<string, NuGetPackageInfo>();

                // Process results in priority order (lower priority number = higher priority)
                foreach (var metadataSet in allMetadata.OrderBy(m => m.Priority))
                {
                    foreach (var metadata in metadataSet.Results)
                    {
                        var version = metadata.Identity.Version.ToString();

                        // Priority-based deduplication: versions from higher priority sources take precedence
                        if (!versionDict.ContainsKey(version))
                        {
                            var packageInfo = new NuGetPackageInfo
                            {
                                Id = metadata.Identity.Id,
                                Version = version,
                                Description = metadata.Description,
                                Authors = metadata.Authors,
                                DownloadCount = metadata.DownloadCount ?? 0,
                                Published = metadata.Published,
                                DependencyGroups = []
                            };

                            // Add dependency groups
                            foreach (var group in metadata.DependencySets)
                            {
                                var frameworkName = group.TargetFramework.ToString();
                                if (!MatchesFramework(frameworkName, includeFrameworkPredicates, excludeFrameworkPredicates))
                                {
                                    continue;
                                }

                                var dependencyGroup = new NuGetPackageDependencyGroup
                                {
                                    TargetFramework = frameworkName,
                                    Dependencies = []
                                };

                                foreach (var dependency in group.Packages)
                                {
                                    dependencyGroup.Dependencies.Add(new NuGetPackageDependency
                                    {
                                        Id = dependency.Id,
                                        VersionRange = dependency.VersionRange.ToString()
                                    });
                                }

                                packageInfo.DependencyGroups.Add(dependencyGroup);
                            }

                            if ((includeFrameworkPredicates.Count > 0 || excludeFrameworkPredicates.Count > 0) &&
                                packageInfo.DependencyGroups.Count == 0)
                            {
                                continue;
                            }

                            versionDict[version] = packageInfo;
                        }
                    }
                }

                var versions = versionDict.Values.ToList();

                // Apply additional filtering if needed
                if (filters.Any())
                {
                    var predicates = filters.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter)).ToList();
                    versions = versions
                        .Where(v => predicates.Any(predicate =>
                            predicate.Invoke(v.Version) ||
                            (v.Description != null && predicate.Invoke(v.Description))))
                        .ToList();
                }

                versions = OrderVersionResults(versions, sortBy, sortDirection);

                // Apply pagination
                var (paged, availablePages) = PaginationHelper.FilterAndPaginate(
                    versions,
                    _ => true,
                    pageNumber,
                    pageSize);

                return new NuGetPackageVersionsResponse
                {
                    PackageId = packageId,
                    Versions = paged,
                    CurrentPage = pageNumber,
                    AvailablePages = availablePages,
                    TotalItems = versions.Count,
                    PageSize = pageSize,
                    SortBy = NormalizeVersionSortBy(sortBy),
                    SortDirection = NormalizeSortDirection(sortDirection)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions for NuGet package: {PackageId}", packageId);
                throw;
            }
        }

        private static List<NuGetPackageInfo> OrderSearchResults(List<NuGetPackageInfo> packages, string sortBy, string sortDirection)
        {
            var normalizedSortBy = NormalizeSearchSortBy(sortBy);
            var isDescending = NormalizeSortDirection(sortDirection) == SortDirections.Desc;

            IOrderedEnumerable<NuGetPackageInfo> ordered = normalizedSortBy switch
            {
                NuGetSearchSortFields.Relevance => packages.OrderBy(_ => 0),
                NuGetSearchSortFields.Version => isDescending
                    ? packages.OrderByDescending(p => ParseVersion(p.Version))
                    : packages.OrderBy(p => ParseVersion(p.Version)),
                NuGetSearchSortFields.Downloads => isDescending
                    ? packages.OrderByDescending(p => p.DownloadCount)
                    : packages.OrderBy(p => p.DownloadCount),
                NuGetSearchSortFields.Published => isDescending
                    ? packages.OrderByDescending(p => p.Published ?? DateTimeOffset.MinValue)
                    : packages.OrderBy(p => p.Published ?? DateTimeOffset.MinValue),
                _ => isDescending
                    ? packages.OrderByDescending(p => p.Id, StringComparer.OrdinalIgnoreCase)
                    : packages.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            };

            var stableOrdered = ordered
                .ThenBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => ParseVersion(p.Version));

            return normalizedSortBy == NuGetSearchSortFields.Relevance
                ? packages
                : stableOrdered.ToList();
        }

        private static List<NuGetPackageInfo> OrderVersionResults(List<NuGetPackageInfo> versions, string sortBy, string sortDirection)
        {
            var normalizedSortBy = NormalizeVersionSortBy(sortBy);
            var isDescending = NormalizeSortDirection(sortDirection) == SortDirections.Desc;

            IOrderedEnumerable<NuGetPackageInfo> ordered = normalizedSortBy switch
            {
                NuGetVersionSortFields.Relevance => versions.OrderBy(_ => 0),
                NuGetVersionSortFields.Downloads => isDescending
                    ? versions.OrderByDescending(v => v.DownloadCount)
                    : versions.OrderBy(v => v.DownloadCount),
                NuGetVersionSortFields.Published => isDescending
                    ? versions.OrderByDescending(v => v.Published ?? DateTimeOffset.MinValue)
                    : versions.OrderBy(v => v.Published ?? DateTimeOffset.MinValue),
                _ => isDescending
                    ? versions.OrderByDescending(v => ParseVersion(v.Version))
                    : versions.OrderBy(v => ParseVersion(v.Version))
            };

            var stableOrdered = ordered
                .ThenBy(v => v.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(v => ParseVersion(v.Version));

            return normalizedSortBy == NuGetVersionSortFields.Relevance
                ? versions
                : stableOrdered.ToList();
        }

        private static NuGetVersion ParseVersion(string version)
        {
            return NuGetVersion.TryParse(version, out var parsed)
                ? parsed
                : new NuGetVersion(0, 0, 0);
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            return string.Equals(sortDirection, SortDirections.Desc, StringComparison.OrdinalIgnoreCase)
                ? SortDirections.Desc
                : SortDirections.Asc;
        }

        private static string NormalizeSearchSortBy(string sortBy)
        {
            return sortBy?.ToLowerInvariant() switch
            {
                NuGetSearchSortFields.Relevance => NuGetSearchSortFields.Relevance,
                NuGetSearchSortFields.Version => NuGetSearchSortFields.Version,
                NuGetSearchSortFields.Downloads => NuGetSearchSortFields.Downloads,
                NuGetSearchSortFields.Published => NuGetSearchSortFields.Published,
                NuGetSearchSortFields.Id => NuGetSearchSortFields.Id,
                _ => NuGetSearchSortFields.Relevance
            };
        }

        private static string NormalizeVersionSortBy(string sortBy)
        {
            return sortBy?.ToLowerInvariant() switch
            {
                NuGetVersionSortFields.Relevance => NuGetVersionSortFields.Relevance,
                NuGetVersionSortFields.Downloads => NuGetVersionSortFields.Downloads,
                NuGetVersionSortFields.Published => NuGetVersionSortFields.Published,
                NuGetVersionSortFields.Version => NuGetVersionSortFields.Version,
                _ => NuGetVersionSortFields.Relevance
            };
        }

        private static bool MatchesFramework(string framework,
            List<Predicate<string>> includeFrameworkPredicates,
            List<Predicate<string>> excludeFrameworkPredicates)
        {
            var included = includeFrameworkPredicates.Count == 0 || includeFrameworkPredicates.Any(predicate => predicate.Invoke(framework));
            var excluded = excludeFrameworkPredicates.Any(predicate => predicate.Invoke(framework));
            return included && !excluded;
        }

        private static bool MatchesFrameworkFilters(
            List<string> frameworks,
            List<Predicate<string>> includeFrameworkPredicates,
            List<Predicate<string>> excludeFrameworkPredicates)
        {
            if (frameworks.Count == 0)
            {
                return includeFrameworkPredicates.Count == 0;
            }

            return frameworks.Any(framework => MatchesFramework(framework, includeFrameworkPredicates, excludeFrameworkPredicates));
        }
    }
}
