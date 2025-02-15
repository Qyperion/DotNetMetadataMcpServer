using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;
using System.Text.RegularExpressions;

namespace DotNetMetadataMcpServer.Services
{
    public class NamespaceToolService
    {
        private readonly DependenciesScanner _scanner;

        public NamespaceToolService(DependenciesScanner scanner)
        {
            _scanner = scanner;
        }

        // Changed signature: now accepts a projectFileAbsolutePath and a list of allowed assembly names.
        public NamespaceToolResponse GetNamespaces(string projectFileAbsolutePath,
            List<string> allowedAssemblyNames, List<string> filters, int pageNumber, int pageSize)
        {
            var metadata = _scanner.ScanProject(projectFileAbsolutePath);

            var allowedAssemblyNamesWithoutExtension = allowedAssemblyNames
                .Select(Path.GetFileNameWithoutExtension)
                .Where(s => s != null)
                .Select(s => s!.ToLowerInvariant())
                .ToHashSet();

            // Build the allowed namespaces only from types of assemblies matching allowedAssemblyNames.
            var allowedNamespaces = new List<string>();
            if (allowedAssemblyNames is { Count: > 0 })
            {
                // Include namespaces from the main project if its assembly name is allowed.
                var mainAssemblyName = Path.GetFileName(metadata.AssemblyPath);
                if (allowedAssemblyNamesWithoutExtension.Contains(mainAssemblyName))
                {
                    allowedNamespaces.AddRange(ExtractNamespaces(metadata.ProjectTypes));
                }

                // Include namespaces from dependencies whose Name is in allowedAssemblyNames.
                foreach (var dep in metadata.Dependencies)
                {
                    if (allowedAssemblyNamesWithoutExtension.Contains(dep.Name))
                    {
                        allowedNamespaces.AddRange(ExtractNamespaces(dep.Types));
                    }
                }
            }
            else
            {
                allowedNamespaces.AddRange(ExtractNamespaces(metadata.ProjectTypes));

                foreach (var dep in metadata.Dependencies)
                {
                    allowedNamespaces.AddRange(ExtractNamespaces(dep.Types));
                }
            }

            // Remove duplicates.
            var allNamespaces = allowedNamespaces.Distinct();

            // Apply additional filter if provided.
            if (filters.Any())
            {
                var pattern = "^" + Regex.Escape(filters[0]).Replace("\\*", ".*") + "$";
                allNamespaces = allNamespaces.Where(n => Regex.IsMatch(n, pattern, RegexOptions.IgnoreCase));
            }

            // Paginate the namespaces.
            var (paged, availablePages) = PaginationHelper.FilterAndPaginate(allNamespaces, _ => true, pageNumber, pageSize);
            return new NamespaceToolResponse
            {
                Namespaces = paged,
                CurrentPage = pageNumber,
                AvailablePages = availablePages
            };
        }

        private static IEnumerable<string> ExtractNamespaces(IEnumerable<TypeInfoModel> types)
        {
            return types
                .Where(t => !string.IsNullOrWhiteSpace(t.FullName) && t.FullName.Contains('.'))
                .Select(t => t.FullName.Substring(0, t.FullName.LastIndexOf('.')));
        }
    }
}