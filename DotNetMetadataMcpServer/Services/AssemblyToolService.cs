using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;

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

        public AssemblyToolResponse GetAssemblies(string projectFileAbsolutePath, List<string> filters, int pageNumber, int pageSize)
        {
            var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
            // Get main assembly name and dependency names from full data
            var assemblies = new List<string> { Path.GetFileNameWithoutExtension(metadata.AssemblyPath) };
            assemblies.AddRange(metadata.Dependencies.Select(d => d.Name));

            if (filters.Any())
            {
                var predicates = filters.Select(FilteringHelper.PrepareFilteringPredicate).ToList();
                assemblies = assemblies.Where(a => predicates.Any(predicate => predicate.Invoke(a))).ToList();
            }
            
            var (paged, availablePages) = PaginationHelper.FilterAndPaginate(assemblies, _ => true, pageNumber, pageSize);
            return new AssemblyToolResponse
            {
                AssemblyNames = paged,
                CurrentPage = pageNumber,
                AvailablePages = availablePages
            };
        }
    }
}