using System.Collections.Concurrent;

namespace DotNetMetadataMcpServer;

/// <summary>
/// Thread-safe, singleton cache for <see cref="ProjectMetadata"/> instances.
/// Uses <see cref="Lazy{T}"/> values to guarantee that the factory runs at most once per key,
/// even under concurrent access from multiple scoped service instances.
/// </summary>
public class ProjectMetadataCache : IProjectMetadataCache
{
    private readonly ConcurrentDictionary<string, Lazy<ProjectMetadata>> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public ProjectMetadata GetOrAdd(string csprojPath, Func<string, ProjectMetadata> factory)
    {
        var normalizedPath = Path.GetFullPath(csprojPath);
        var lazy = _cache.GetOrAdd(normalizedPath, key => new Lazy<ProjectMetadata>(() => factory(key)));
        return lazy.Value;
    }

    /// <inheritdoc />
    public bool Invalidate(string csprojPath)
    {
        var normalizedPath = Path.GetFullPath(csprojPath);
        return _cache.TryRemove(normalizedPath, out _);
    }

    /// <inheritdoc />
    public void InvalidateAll() => _cache.Clear();
}
