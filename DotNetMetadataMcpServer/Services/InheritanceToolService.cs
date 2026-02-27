using DotNetMetadataMcpServer.Models;

namespace DotNetMetadataMcpServer.Services;

public class InheritanceToolService
{
    private readonly IDependenciesScanner _scanner;
    private readonly IProjectMetadataCache _cache;

    public InheritanceToolService(IDependenciesScanner scanner, IProjectMetadataCache cache)
    {
        _scanner = scanner;
        _cache = cache;
    }

    public InheritanceHierarchyResponse GetHierarchy(string projectFileAbsolutePath, string typeQuery)
    {
        if (string.IsNullOrWhiteSpace(typeQuery))
        {
            throw new ArgumentException("Type query must not be empty.", nameof(typeQuery));
        }

        var metadata = _cache.GetOrAdd(projectFileAbsolutePath, path => _scanner.ScanProject(path));
        var allTypes = metadata.ProjectTypes.Concat(metadata.Dependencies.SelectMany(d => d.Types)).ToList();

        var typeLookup = allTypes
            .Where(t => !string.IsNullOrWhiteSpace(t.FullName))
            .GroupBy(t => t.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(t => t.FullName, StringComparer.OrdinalIgnoreCase);

        var requestedType = ResolveType(typeLookup.Values, typeQuery);
        if (requestedType is null)
        {
            throw new InvalidOperationException($"Type '{typeQuery}' was not found in project or dependency assemblies.");
        }

        var baseTypeChain = BuildBaseTypeChain(typeLookup, requestedType);
        var derivedTypes = typeLookup.Values
            .Where(t => !string.Equals(t.FullName, requestedType.FullName, StringComparison.OrdinalIgnoreCase))
            .Where(t => IsDerivedFrom(typeLookup, t, requestedType.FullName))
            .Select(t => t.FullName)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new InheritanceHierarchyResponse
        {
            TypeFullName = requestedType.FullName,
            DirectBaseType = string.IsNullOrWhiteSpace(requestedType.BaseType) ? null : requestedType.BaseType,
            BaseTypeChain = baseTypeChain,
            DerivedTypes = derivedTypes
        };
    }

    private static TypeInfoModel? ResolveType(IEnumerable<TypeInfoModel> types, string typeQuery)
    {
        return types.FirstOrDefault(t =>
                   string.Equals(t.FullName, typeQuery, StringComparison.OrdinalIgnoreCase))
               ?? types.FirstOrDefault(t =>
                   string.Equals(GetTypeShortName(t.FullName), typeQuery, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> BuildBaseTypeChain(
        IReadOnlyDictionary<string, TypeInfoModel> typeLookup,
        TypeInfoModel type)
    {
        var chain = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentBaseType = type.BaseType;

        while (!string.IsNullOrWhiteSpace(currentBaseType) && visited.Add(currentBaseType))
        {
            chain.Add(currentBaseType);

            if (!typeLookup.TryGetValue(currentBaseType, out var baseTypeModel))
            {
                break;
            }

            currentBaseType = baseTypeModel.BaseType;
        }

        return chain;
    }

    private static bool IsDerivedFrom(
        IReadOnlyDictionary<string, TypeInfoModel> typeLookup,
        TypeInfoModel candidate,
        string targetBaseType)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentBaseType = candidate.BaseType;

        while (!string.IsNullOrWhiteSpace(currentBaseType) && visited.Add(currentBaseType))
        {
            if (string.Equals(currentBaseType, targetBaseType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!typeLookup.TryGetValue(currentBaseType, out var baseTypeModel))
            {
                return false;
            }

            currentBaseType = baseTypeModel.BaseType;
        }

        return false;
    }

    private static string GetTypeShortName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }
}
