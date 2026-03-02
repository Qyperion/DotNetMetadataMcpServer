using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;

namespace DotNetMetadataMcpServer.Services
{
    public class NamespaceToolService
    {
        private readonly IDependenciesScanner _scanner;
        private readonly IProjectMetadataCache _cache;

        public NamespaceToolService(IDependenciesScanner scanner, IProjectMetadataCache cache)
        {
            _scanner = scanner;
            _cache = cache;
        }

        // Changed signature: now accepts a projectFileAbsolutePath and a list of allowed assembly names.
        public NamespaceToolResponse GetNamespaces(string projectFileAbsolutePath,
            List<string> allowedAssemblyNames, List<string> filters, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
            var flattenedDependencies = FlattenDependencies(metadata.Dependencies).ToList();

            var allowedAssemblyNamesWithoutExtension = allowedAssemblyNames
                .Select(NormalizeAssemblyName)
                .Select(s => s.ToLowerInvariant())
                .ToHashSet();

            // Build the allowed namespaces only from types of assemblies matching allowedAssemblyNames.
            var allowedNamespaces = new List<string>();
            if (allowedAssemblyNames is { Count: > 0 })
            {
                // Include namespaces from the main project if its assembly name is allowed.
                var mainAssemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(metadata.AssemblyPath);
                if (allowedAssemblyNamesWithoutExtension.Contains(mainAssemblyNameWithoutExtension.ToLowerInvariant()))
                {
                    allowedNamespaces.AddRange(ExtractNamespaces(metadata.ProjectTypes));
                }

                // Include namespaces from dependencies whose Name is in allowedAssemblyNames.
                foreach (var dep in flattenedDependencies)
                {
                    var depNameWithoutExtension = NormalizeAssemblyName(dep.Name);
                    if (allowedAssemblyNamesWithoutExtension.Contains(depNameWithoutExtension.ToLowerInvariant()))
                    {
                        allowedNamespaces.AddRange(ExtractNamespaces(dep.Types));
                    }
                }
            }
            else
            {
                allowedNamespaces.AddRange(ExtractNamespaces(metadata.ProjectTypes));

                foreach (var dep in flattenedDependencies)
                {
                    allowedNamespaces.AddRange(ExtractNamespaces(dep.Types));
                }
            }

            // Remove duplicates.
            var allNamespaces = allowedNamespaces.Distinct();

            // Apply additional filter if provided.
            if (filters.Any())
            {
                var predicates = filters.Select(filter => FilteringHelper.PrepareFilteringPredicate(filter)).ToList();
                allNamespaces = allNamespaces.Where(n => predicates.Any(predicate => predicate.Invoke(n)));
            }

            var allNamespacesList = allNamespaces.ToList();

            // Paginate the namespaces.
            var (paged, availablePages) = PaginationHelper.FilterAndPaginate(allNamespacesList, _ => true, pageNumber, pageSize);
            return new NamespaceToolResponse
            {
                Namespaces = paged,
                CurrentPage = pageNumber,
                AvailablePages = availablePages,
                TotalItems = allNamespacesList.Count,
                PageSize = pageSize
            };
        }

        private static IEnumerable<string> ExtractNamespaces(IEnumerable<TypeInfoModel> types)
        {
            return types
                .Where(t => !string.IsNullOrWhiteSpace(t.FullName) && t.FullName.Contains('.'))
                .Select(t => t.FullName[..t.FullName.LastIndexOf('.')]);
        }

        /// <summary>
        /// Strips only .dll/.exe extensions from assembly names.
        /// Unlike Path.GetFileNameWithoutExtension, this preserves dotted names
        /// like "Newtonsoft.Json" or "McpTestProject.Core".
        /// </summary>
        private static string NormalizeAssemblyName(string name)
        {
            if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return name[..^4];
            }

            return name;
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
