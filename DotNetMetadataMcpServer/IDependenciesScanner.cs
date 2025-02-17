namespace DotNetMetadataMcpServer;

public interface IDependenciesScanner : IDisposable
{
    /// <summary>
    /// Scans .csproj:
    /// 1. MSBuild → assemblyPath, assetsFilePath
    /// 2. Loads public types from the project itself (assemblyPath)
    /// 3. Parses project.assets.json, builds DependencyGraph
    /// 4. Loads assemblies for packages (via .RuntimeAssemblies)
    /// 5. Returns ProjectMetadata
    /// </summary>
    ProjectMetadata ScanProject(string csprojPath);
}