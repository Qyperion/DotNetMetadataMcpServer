using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;

namespace DotNetMetadataMcpServer.Services;

public class TypeToolService
{
    private readonly IDependenciesScanner _scanner;
    private readonly IProjectMetadataCache _cache;

    public TypeToolService(IDependenciesScanner scanner, IProjectMetadataCache cache)
    {
        _scanner = scanner;
        _cache = cache;
    }

    // Changed signature: now accepts a projectFileAbsolutePath and an allowed list of namespaces.
    public TypeToolResponse GetTypes(string projectFileAbsolutePath,
        List<string> allowedNamespaces, List<string> filters, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
        // Collect all types from project and dependencies.
        var dependencyTypes = FlattenDependencies(metadata.Dependencies).SelectMany(d => d.Types);
        var allTypes = metadata.ProjectTypes.Concat(dependencyTypes);

        // If allowed namespaces are provided, only retain types whose namespace (the part before the last '.') is allowed.
        if (allowedNamespaces.Any())
        {
            allTypes = allTypes.Where(t =>
                !string.IsNullOrEmpty(t.FullName) &&
                t.FullName.Contains('.') &&
                allowedNamespaces.Contains(t.FullName[..t.FullName.LastIndexOf('.')], StringComparer.OrdinalIgnoreCase)
            );
        }

        // Apply additional filter if provided.
        if (filters.Any())
        {
            var predicates = filters.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter)).ToList();
            allTypes = allTypes.Where(t => predicates.Any(predicate => predicate.Invoke(t.FullName)));
        }

        var allTypesList = allTypes.Select(TypeInfoModelMapper.ToSimpleTypeInfo).ToList();
        var (paged, availablePages) = PaginationHelper.FilterAndPaginate(allTypesList, _ => true, pageNumber, pageSize);

        return new TypeToolResponse
        {
            TypeData = paged,
            CurrentPage = pageNumber,
            AvailablePages = availablePages,
            TotalItems = allTypesList.Count,
            PageSize = pageSize
        };
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