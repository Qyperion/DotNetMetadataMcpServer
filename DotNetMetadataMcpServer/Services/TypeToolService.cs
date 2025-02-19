using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;
using System.Text.RegularExpressions;

namespace DotNetMetadataMcpServer.Services
{
    public class TypeToolService
    {
        private readonly IDependenciesScanner _scanner;
        
        public TypeToolService(IDependenciesScanner scanner)
        {
            _scanner = scanner;
        }

        // Changed signature: now accepts a projectFileAbsolutePath and an allowed list of namespaces.
        public TypeToolResponse GetTypes(string projectFileAbsolutePath, 
            List<string> allowedNamespaces, List<string> filters, int pageNumber, int pageSize)
        {
            var metadata = _scanner.ScanProject(projectFileAbsolutePath);
            // Collect all types from project and dependencies.
            var allTypes = metadata.ProjectTypes.Concat(metadata.Dependencies.SelectMany(d => d.Types));
            
            // If allowed namespaces are provided, only retain types whose namespace (the part before the last '.') is allowed.
            if (allowedNamespaces.Any())
            {
                allTypes = allTypes.Where(t => 
                    !string.IsNullOrEmpty(t.FullName) && 
                    t.FullName.Contains('.') && 
                    allowedNamespaces.Contains(t.FullName.Substring(0, t.FullName.LastIndexOf('.')), StringComparer.OrdinalIgnoreCase)
                );
            }
            
            // Apply additional filter if provided.
            if (filters.Any())
            {
                var predicates = filters.Select(FilteringHelper.PrepareFilteringPredicate).ToList();
                allTypes = allTypes.Where(t => predicates.Any(predicate => predicate.Invoke(t.FullName)));
            }
            
            var allTypesList = allTypes.Select(TypeInfoModelMapper.ToSimpleTypeInfo).ToList();
            var (paged, availablePages) = PaginationHelper.FilterAndPaginate(allTypesList, _ => true, pageNumber, pageSize);
            
            return new TypeToolResponse
            {
                TypeData = paged,
                CurrentPage = pageNumber,
                AvailablePages = availablePages
            };
        }
    }
}