using System.Text.RegularExpressions;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;

namespace DotNetMetadataMcpServer.Services
{
    public class AssemblyToolService
    {
        private readonly DependenciesScanner _scanner;

        public AssemblyToolService(DependenciesScanner scanner)
        {
            _scanner = scanner;
        }

        public AssemblyToolResponse GetAssemblies(string projectFileAbsolutePath, List<string> filters, int pageNumber, int pageSize)
        {
            var metadata = _scanner.ScanProject(projectFileAbsolutePath);
            // Get main assembly name and dependency names from full data
            var assemblies = new List<string> { Path.GetFileName(metadata.AssemblyPath) };
            assemblies.AddRange(metadata.Dependencies.Select(d => d.Name));

            if (filters.Any())
            {
                var pattern = "^" + Regex.Escape(filters[0]).Replace("\\*", ".*") + "$";
                assemblies = assemblies.Where(a => Regex.IsMatch(a, pattern, RegexOptions.IgnoreCase)).ToList();
            }

            var paged = PaginationHelper.Paginate(assemblies, pageNumber, pageSize, out var availablePages);
            return new AssemblyToolResponse
            {
                AssemblyNames = paged,
                CurrentPage = pageNumber,
                AvailablePages = availablePages
            };
        }
    }
}