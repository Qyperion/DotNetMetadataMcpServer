namespace DotNetMetadataMcpServer;

/// <summary>
/// Thread-safe cache for <see cref="ProjectMetadata"/> results produced by <see cref="IDependenciesScanner"/>.
/// Registered as a Singleton so that the cache is shared across all scoped service instances.
/// </summary>
public interface IProjectMetadataCache
{
    /// <summary>
    /// Returns cached metadata for the given .csproj path, or invokes <paramref name="factory"/> exactly once
    /// (even under concurrent access) and caches the result.
    /// </summary>
    ProjectMetadata GetOrAdd(string csprojPath, Func<string, ProjectMetadata> factory);

    /// <summary>
    /// Removes the cached entry for a specific project path.
    /// </summary>
    bool Invalidate(string csprojPath);

    /// <summary>
    /// Clears the entire cache.
    /// </summary>
    void InvalidateAll();
}
