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

    public DependencyGraphToolResponse GetDependencyGraph(string projectFileAbsolutePath)
    {
        var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
        var graphNodes = metadata.Dependencies.Select(MapNode).ToList();

        return new DependencyGraphToolResponse
        {
            Dependencies = graphNodes,
            TotalNodes = CountNodes(graphNodes)
        };
    }

    private static DependencyGraphNodeResponse MapNode(DependencyInfo dependency)
    {
        return new DependencyGraphNodeResponse
        {
            Name = dependency.Name,
            Version = string.IsNullOrWhiteSpace(dependency.Version) ? null : dependency.Version,
            NodeType = dependency.NodeType,
            TypeCount = dependency.Types.Count,
            Children = dependency.Children.Select(MapNode).ToList()
        };
    }

    private static int CountNodes(IEnumerable<DependencyGraphNodeResponse> nodes)
    {
        return nodes.Sum(node => 1 + CountNodes(node.Children));
    }
}
