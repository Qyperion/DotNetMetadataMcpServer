using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Models.Base;

namespace DotNetMetadataMcpServer.Services
{
    public class AssemblyToolService
    {
        private readonly IDependenciesScanner _scanner;
        private readonly IProjectMetadataCache _cache;

        public AssemblyToolService(IDependenciesScanner scanner, IProjectMetadataCache cache)
        {
            _scanner = scanner;
            _cache = cache;
        }

        public AssemblyToolResponse GetAssemblies(string projectFileAbsolutePath, List<string> filters, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
            // Get main assembly name and dependency names from full data
            var assemblies = new List<string> { Path.GetFileNameWithoutExtension(metadata.AssemblyPath) };
            assemblies.AddRange(FlattenDependencies(metadata.Dependencies)
                .Where(d => string.IsNullOrEmpty(d.NodeType) || d.NodeType == DependencyNodeTypes.Package)
                .Select(d => d.Name));

            assemblies = assemblies
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (filters.Any())
            {
                var predicates = filters.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter)).ToList();
                assemblies = assemblies.Where(a => predicates.Any(predicate => predicate.Invoke(a))).ToList();
            }
            
            var (paged, availablePages) = PaginationHelper.FilterAndPaginate(assemblies, _ => true, pageNumber, pageSize);
            return new AssemblyToolResponse
            {
                AssemblyNames = paged,
                CurrentPage = pageNumber,
                AvailablePages = availablePages,
                TotalItems = assemblies.Count,
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
}
