using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;

namespace DotNetMetadataMcpServer.Services;

public class DependencyGraphToolService
{
    private readonly IDependenciesScanner _scanner;
    private readonly IProjectMetadataCache _cache;

    public DependencyGraphToolService(IDependenciesScanner scanner, IProjectMetadataCache cache)
    {
        _scanner = scanner;
        _cache = cache;
    }

    public DependencyGraphToolResponse GetDependencyGraph(
        string projectFileAbsolutePath,
        List<string> includeFilters,
        List<string> excludeFilters,
        int maxDepth,
        string viewMode)
    {
        var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
        var includePredicates = includeFilters.Select(FilteringHelper.PrepareFilteringPredicate).ToList();
        var excludePredicates = excludeFilters.Select(FilteringHelper.PrepareFilteringPredicate).ToList();

        var graphNodes = metadata.Dependencies
            .Select(d => MapNode(d, 1, null, includePredicates, excludePredicates, maxDepth))
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();

        var normalizedMode = NormalizeViewMode(viewMode);
        if (normalizedMode == "flat")
        {
            graphNodes = FlattenNodes(graphNodes).ToList();
        }

        return new DependencyGraphToolResponse
        {
            ViewMode = normalizedMode,
            Dependencies = graphNodes,
            TotalNodes = CountNodes(graphNodes)
        };
    }

    private static DependencyGraphNodeResponse? MapNode(
        DependencyInfo dependency,
        int depth,
        string? parentName,
        List<Predicate<string>> includePredicates,
        List<Predicate<string>> excludePredicates,
        int maxDepth)
    {
        if (maxDepth > 0 && depth > maxDepth)
        {
            return null;
        }

        var mappedChildren = dependency.Children
            .Select(c => MapNode(c, depth + 1, dependency.Name, includePredicates, excludePredicates, maxDepth))
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        var includedByIncludeFilter = includePredicates.Count == 0 || includePredicates.Any(p => p.Invoke(dependency.Name));
        var excludedByExcludeFilter = excludePredicates.Any(p => p.Invoke(dependency.Name));
        var includeCurrentNode = includedByIncludeFilter && !excludedByExcludeFilter;

        if (!includeCurrentNode && mappedChildren.Count == 0)
        {
            return null;
        }

        return new DependencyGraphNodeResponse
        {
            Name = dependency.Name,
            Version = string.IsNullOrWhiteSpace(dependency.Version) ? null : dependency.Version,
            NodeType = dependency.NodeType,
            TypeCount = dependency.Types.Count,
            Depth = depth,
            ParentName = parentName,
            Children = mappedChildren
        };
    }

    private static IEnumerable<DependencyGraphNodeResponse> FlattenNodes(IEnumerable<DependencyGraphNodeResponse> nodes)
    {
        foreach (var node in nodes)
        {
            yield return new DependencyGraphNodeResponse
            {
                Name = node.Name,
                Version = node.Version,
                NodeType = node.NodeType,
                TypeCount = node.TypeCount,
                Depth = node.Depth,
                ParentName = node.ParentName,
                Children = []
            };

            foreach (var child in FlattenNodes(node.Children))
            {
                yield return child;
            }
        }
    }

    private static int CountNodes(IEnumerable<DependencyGraphNodeResponse> nodes)
    {
        return nodes.Sum(node => 1 + CountNodes(node.Children));
    }

    private static string NormalizeViewMode(string viewMode)
    {
        return string.Equals(viewMode, "flat", StringComparison.OrdinalIgnoreCase)
            ? "flat"
            : "tree";
    }
}
