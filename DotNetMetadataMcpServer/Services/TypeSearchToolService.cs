using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;

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
        int pageNumber,
        int pageSize)
    {
        var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
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

        foreach (var dep in metadata.Dependencies)
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
            matchingTypes = matchingTypes.Where(t =>
                t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                GetTypeShortName(t.FullName).Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        if (filters.Count > 0)
        {
            var predicates = filters.Select(FilteringHelper.PrepareFilteringPredicate).ToList();
            matchingTypes = matchingTypes.Where(t => predicates.Any(predicate => predicate.Invoke(t.FullName)));
        }

        var orderedTypes = matchingTypes
            .OrderBy(t => t.FullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.AssemblyName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var (paged, availablePages) = PaginationHelper.FilterAndPaginate(orderedTypes, _ => true, pageNumber, pageSize);

        return new TypeSearchToolResponse
        {
            TypeMatches = paged,
            CurrentPage = pageNumber,
            AvailablePages = availablePages
        };
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
}
