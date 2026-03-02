using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Models.Base;

namespace DotNetMetadataMcpServer.Services;

public class TypeSearchToolService
{
    private readonly IDependenciesScanner _scanner;
    private readonly IProjectMetadataCache _cache;

    public TypeSearchToolService(IDependenciesScanner scanner, IProjectMetadataCache cache)
    {
        _scanner = scanner;
        _cache = cache;
    }

    public TypeSearchToolResponse SearchTypes(
        string projectFileAbsolutePath,
        string searchQuery,
        List<string> allowedAssemblyNames,
        List<string> filters,
        string sortBy,
        string sortDirection,
        int pageNumber,
        int pageSize)
    {
        return SearchTypes(
            projectFileAbsolutePath,
            searchQuery,
            allowedAssemblyNames,
            filters,
            false,
            sortBy,
            sortDirection,
            pageNumber,
            pageSize,
            CancellationToken.None);
    }

    public TypeSearchToolResponse SearchTypes(
        string projectFileAbsolutePath,
        string searchQuery,
        List<string> allowedAssemblyNames,
        List<string> filters,
        bool caseSensitive,
        string sortBy,
        string sortDirection,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
        var flattenedDependencies = FlattenDependencies(metadata.Dependencies).ToList();
        var normalizedAllowedAssemblies = allowedAssemblyNames
            .Select(NormalizeAssemblyName)
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        var mainAssemblyName = Path.GetFileNameWithoutExtension(metadata.AssemblyPath);
        var allTypes = new List<TypeSearchMatch>();

        if (allowedAssemblyNames.Count == 0 || normalizedAllowedAssemblies.Contains(mainAssemblyName.ToLowerInvariant()))
        {
            allTypes.AddRange(metadata.ProjectTypes.Select(t => new TypeSearchMatch
            {
                FullName = t.FullName,
                AssemblyName = mainAssemblyName,
                Documentation = string.IsNullOrEmpty(t.Documentation) ? null : t.Documentation
            }));
        }

        foreach (var dep in flattenedDependencies)
        {
            if (dep.Types.Count == 0)
            {
                continue;
            }

            var normalizedDependencyName = NormalizeAssemblyName(dep.Name);
            if (allowedAssemblyNames.Count > 0 && !normalizedAllowedAssemblies.Contains(normalizedDependencyName.ToLowerInvariant()))
            {
                continue;
            }

            allTypes.AddRange(dep.Types.Select(t => new TypeSearchMatch
            {
                FullName = t.FullName,
                AssemblyName = normalizedDependencyName,
                Documentation = string.IsNullOrEmpty(t.Documentation) ? null : t.Documentation
            }));
        }

        var matchingTypes = allTypes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            matchingTypes = matchingTypes.Where(t =>
                t.FullName.Contains(searchQuery, comparison) ||
                GetTypeShortName(t.FullName).Contains(searchQuery, comparison));
        }

        if (filters.Count > 0)
        {
            var predicates = filters.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter, caseSensitive)).ToList();
            matchingTypes = matchingTypes.Where(t => predicates.Any(predicate => predicate.Invoke(t.FullName)));
        }

        var orderedTypes = Order(matchingTypes, sortBy, sortDirection).ToList();

        var (paged, availablePages) = PaginationHelper.FilterAndPaginate(orderedTypes, _ => true, pageNumber, pageSize);

        return new TypeSearchToolResponse
        {
            TypeMatches = paged,
            CurrentPage = pageNumber,
            AvailablePages = availablePages,
            TotalItems = orderedTypes.Count,
            PageSize = pageSize,
            SortBy = NormalizeSortBy(sortBy),
            SortDirection = NormalizeSortDirection(sortDirection)
        };
    }

    private static IEnumerable<TypeSearchMatch> Order(IEnumerable<TypeSearchMatch> items, string sortBy, string sortDirection)
    {
        var normalizedSortBy = NormalizeSortBy(sortBy);
        var isDescending = string.Equals(NormalizeSortDirection(sortDirection), SortDirections.Desc, StringComparison.OrdinalIgnoreCase);

        IOrderedEnumerable<TypeSearchMatch> ordered = normalizedSortBy switch
        {
            TypeSearchSortFields.AssemblyName => isDescending
                ? items.OrderByDescending(t => t.AssemblyName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(t => t.AssemblyName, StringComparer.OrdinalIgnoreCase),
            _ => isDescending
                ? items.OrderByDescending(t => t.FullName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(t => t.FullName, StringComparer.OrdinalIgnoreCase)
        };

        return normalizedSortBy == TypeSearchSortFields.AssemblyName
            ? ordered.ThenBy(t => t.FullName, StringComparer.OrdinalIgnoreCase)
            : ordered.ThenBy(t => t.AssemblyName, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetTypeShortName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }

    private static string NormalizeAssemblyName(string name)
    {
        if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return name[..^4];
        }

        return name;
    }

    private static string NormalizeSortBy(string sortBy)
    {
        return string.Equals(sortBy, TypeSearchSortFields.AssemblyName, StringComparison.OrdinalIgnoreCase)
            ? TypeSearchSortFields.AssemblyName
            : TypeSearchSortFields.FullName;
    }

    private static string NormalizeSortDirection(string sortDirection)
    {
        return string.Equals(sortDirection, SortDirections.Desc, StringComparison.OrdinalIgnoreCase)
            ? SortDirections.Desc
            : SortDirections.Asc;
    }

    private static IEnumerable<DependencyInfo> FlattenDependencies(IEnumerable<DependencyInfo> dependencies)
    {
        foreach (var dependency in dependencies)
        {
            yield return dependency;

            foreach (var child in FlattenDependencies(dependency.Children))
            {
                yield return child;
            }
        }
    }
}
