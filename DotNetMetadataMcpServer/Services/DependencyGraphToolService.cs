using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Models.Base;

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
        return GetDependencyGraph(
            projectFileAbsolutePath,
            includeFilters,
            excludeFilters,
            [],
            [],
            maxDepth,
            viewMode,
            CancellationToken.None);
    }

    public DependencyGraphToolResponse GetDependencyGraph(
        string projectFileAbsolutePath,
        List<string> includeFilters,
        List<string> excludeFilters,
        List<string> includeFrameworks,
        List<string> excludeFrameworks,
        int maxDepth,
        string viewMode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
        var includePredicates = includeFilters.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter)).ToList();
        var excludePredicates = excludeFilters.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter)).ToList();
        var includeFrameworkPredicates = includeFrameworks.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter)).ToList();
        var excludeFrameworkPredicates = excludeFrameworks.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter)).ToList();

        var graphNodes = metadata.Dependencies
            .Select(d => MapNode(d, 1, null, includePredicates, excludePredicates, includeFrameworkPredicates, excludeFrameworkPredicates, maxDepth))
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();

        var normalizedMode = NormalizeViewMode(viewMode);
        if (normalizedMode == DependencyGraphViewModes.Flat)
        {
            graphNodes = FlattenNodes(graphNodes).ToList();
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new DependencyGraphToolResponse
        {
            ViewMode = normalizedMode,
            Dependencies = graphNodes,
            TotalNodes = CountNodes(graphNodes),
            TotalItems = graphNodes.Count,
            PageSize = graphNodes.Count
        };
    }

    private static DependencyGraphNodeResponse? MapNode(
        DependencyInfo dependency,
        int depth,
        string? parentName,
        List<Predicate<string>> includePredicates,
        List<Predicate<string>> excludePredicates,
        List<Predicate<string>> includeFrameworkPredicates,
        List<Predicate<string>> excludeFrameworkPredicates,
        int maxDepth)
    {
        if (maxDepth > 0 && depth > maxDepth)
        {
            return null;
        }

        var mappedChildren = dependency.Children
            .Select(c => MapNode(c, depth + 1, dependency.Name, includePredicates, excludePredicates, includeFrameworkPredicates, excludeFrameworkPredicates, maxDepth))
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        var includedByIncludeFilter = includePredicates.Count == 0 || includePredicates.Any(p => p.Invoke(dependency.Name));
        var excludedByExcludeFilter = excludePredicates.Any(p => p.Invoke(dependency.Name));
        var includedByFramework = includeFrameworkPredicates.Count == 0 ||
                                  (!string.IsNullOrWhiteSpace(dependency.Framework) &&
                                   includeFrameworkPredicates.Any(p => p.Invoke(dependency.Framework)));
        var excludedByFramework = !string.IsNullOrWhiteSpace(dependency.Framework) &&
                                  excludeFrameworkPredicates.Any(p => p.Invoke(dependency.Framework));

        var includeCurrentNode = includedByIncludeFilter && !excludedByExcludeFilter && includedByFramework && !excludedByFramework;

        if (!includeCurrentNode && mappedChildren.Count == 0)
        {
            return null;
        }

        return new DependencyGraphNodeResponse
        {
            Name = dependency.Name,
            Version = string.IsNullOrWhiteSpace(dependency.Version) ? null : dependency.Version,
            NodeType = ToNodeTypeDto(dependency.NodeType),
            Framework = dependency.Framework,
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
                Framework = node.Framework,
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
        return string.Equals(viewMode, DependencyGraphViewModes.Flat, StringComparison.OrdinalIgnoreCase)
            ? DependencyGraphViewModes.Flat
            : DependencyGraphViewModes.Tree;
    }

    private static DependencyNodeTypeDto ToNodeTypeDto(string? nodeType)
    {
        return nodeType?.ToLowerInvariant() switch
        {
            DependencyNodeTypes.Root => DependencyNodeTypeDto.Root,
            DependencyNodeTypes.TargetFramework => DependencyNodeTypeDto.TargetFramework,
            DependencyNodeTypes.Package => DependencyNodeTypeDto.Package,
            DependencyNodeTypes.Project => DependencyNodeTypeDto.Project,
            _ => DependencyNodeTypeDto.Unknown
        };
    }
}
