using DependencyGraph.Core.Graph;
using DependencyGraph.Core.Graph.Factory;
using DotNetMetadataMcpServer.Models.Base;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.ProjectModel;
using System.Reflection;
using System.Runtime.Loader;

namespace DotNetMetadataMcpServer;

public class DependenciesScanner : IDependenciesScanner
{
    private readonly MsBuildHelper _msbuild;
    private readonly ReflectionTypesCollector _reflection;
    private readonly ILogger _nuGetLogger;
    private readonly ILogger<DependenciesScanner> _logger;

    private readonly HashSet<IDependencyGraphNode> _visitedNodes = [];


    public DependenciesScanner(
        MsBuildHelper msBuildHelper,
        ReflectionTypesCollector reflectionTypesCollector,
        ILogger<DependenciesScanner>? logger = null,
        ILogger<LockFileFormat>? nuGetLogger = null)
    {
        _msbuild = msBuildHelper;
        _reflection = reflectionTypesCollector;
        _nuGetLogger = nuGetLogger ?? NullLogger<LockFileFormat>.Instance;
        _logger = logger ?? NullLogger<DependenciesScanner>.Instance;

        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
    }

    /// <summary>
    /// Scans .csproj:
    /// 1. MSBuild → assemblyPath, assetsFilePath
    /// 2. Loads public types from the project itself (assemblyPath)
    /// 3. Parses project.assets.json, builds DependencyGraph
    /// 4. Loads assemblies for packages (via .RuntimeAssemblies)
    /// 5. Returns ProjectMetadata
    /// </summary>
    public ProjectMetadata ScanProject(string csprojPath)
    {
        _visitedNodes.Clear();

        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        var (asmPath, assetsPath, tfm) = _msbuild.EvaluateProject(csprojPath);

        var baseDir = Path.GetDirectoryName(asmPath) ?? "";

        var projectName = Path.GetFileNameWithoutExtension(csprojPath);
        var pm = new ProjectMetadata
        {
            ProjectName = projectName,
            TargetFramework = tfm,
            AssemblyPath = asmPath
        };
        var depList = new List<DependencyInfo>();
        pm.Dependencies = depList;

        // 1) Load public types from the project itself
        var projectTypes = _reflection.LoadAssemblyTypes(asmPath);
        pm.ProjectTypes.AddRange(projectTypes);

        // 2) If there is no assetsFile, skip dependencies
        if (string.IsNullOrEmpty(assetsPath) || !File.Exists(assetsPath))
        {
            _logger.LogWarning("No project.assets.json found. Skip dependency scanning.");
            return pm;
        }

        // 3) Build DependencyGraph
        var lockFileFormat = new LockFileFormat();
        var lockFile = lockFileFormat.Read(assetsPath, new MicrosoftLoggerAdapter(_nuGetLogger));


        var theFirstTarget = lockFile.Targets.FirstOrDefault();
        if (theFirstTarget == null)
        {
            _logger.LogWarning("No targets found in lock file.");
            return pm;
        }

        var depGraphFactory = new DependencyGraphFactory(new DependencyGraphFactoryOptions
        {
            Excludes = ["Microsoft.*", "System.*"]
        });

        var graph = depGraphFactory.FromLockFile(lockFile);

        var rootNode = graph.RootNodes.OfType<RootProjectDependencyGraphNode>().FirstOrDefault();
        if (rootNode == null)
        {
            _logger.LogWarning("No RootProjectDependencyGraphNode found.");
            foreach (var lib in theFirstTarget.Libraries)
            {
                var d = BuildDependencyInfo(lib, baseDir);
                depList.Add(d);
            }

            return pm;
        }

        var tfmNode = rootNode.Dependencies.OfType<TargetFrameworkDependencyGraphNode>().FirstOrDefault();
        if (tfmNode == null)
        {
            _logger.LogWarning("No TargetFrameworkDependencyGraphNode found under root.");
            foreach (var lib in theFirstTarget.Libraries)
            {
                var d = BuildDependencyInfo(lib, baseDir);
                depList.Add(d);
            }

            return pm;
        }

        foreach (var child in tfmNode.Dependencies)
        {
            var d = BuildDependencyInfo(child, baseDir, tfmNode.TargetFrameworkIdentifier);
            if (d != null)
            {
                depList.Add(d);
            }
        }


        return pm;
    }

    private DependencyInfo BuildDependencyInfo(LockFileTargetLibrary lockFileTargetLibrary, string baseDir)
    {
        var info = new DependencyInfo
        {
            Name = lockFileTargetLibrary.Name ?? "Unknown",
            Version = lockFileTargetLibrary.Version?.ToNormalizedString() ?? "",
            NodeType = DependencyNodeTypes.Package,
            Framework = null
        };

        foreach (var lockFileItem in lockFileTargetLibrary.RuntimeAssemblies)
        {
            var rel = lockFileItem.Path; // e.g., "lib/net10.0/FluentValidation.dll"
            info.Framework ??= ExtractFrameworkFromRuntimeAssemblyPath(rel);
            var fileName = Path.GetFileName(rel);
            var full = Path.Combine(baseDir, fileName);
            var types = _reflection.LoadAssemblyTypes(full);
            info.Types.AddRange(types);
        }

        return info;
    }

    private DependencyInfo? BuildDependencyInfo(IDependencyGraphNode node, string baseDir, string? framework)
    {
        // Check if already visited
        if (!_visitedNodes.Add(node))
            return null;

        switch (node)
        {
            case RootProjectDependencyGraphNode rootNode:
                {
                    var info = new DependencyInfo
                    {
                        Name = rootNode.Name,
                        NodeType = DependencyNodeTypes.Root,
                        Framework = framework
                    };

                    foreach (var child in rootNode.Dependencies)
                    {
                        var c = BuildDependencyInfo(child, baseDir, framework);
                        if (c != null)
                            info.Children.Add(c);
                    }

                    return info;
                }
            case TargetFrameworkDependencyGraphNode tfmNode:
                {
                    var info = new DependencyInfo
                    {
                        Name = tfmNode.ProjectName,
                        Version = tfmNode.TargetFrameworkIdentifier,
                        NodeType = DependencyNodeTypes.TargetFramework,
                        Framework = tfmNode.TargetFrameworkIdentifier
                    };

                    foreach (var child in tfmNode.Dependencies)
                    {
                        var c = BuildDependencyInfo(child, baseDir, tfmNode.TargetFrameworkIdentifier);
                        if (c != null)
                            info.Children.Add(c);
                    }

                    return info;
                }
            case PackageDependencyGraphNode pkgNode:
                {
                    var info = new DependencyInfo
                    {
                        Name = pkgNode.Name,
                        Version = pkgNode.Version.ToNormalizedString(),
                        NodeType = DependencyNodeTypes.Package,
                        Framework = framework
                    };

                    // Load RuntimeAssemblies
                    foreach (var asmItem in pkgNode.TargetLibrary.RuntimeAssemblies)
                    {
                        var rel = asmItem.Path; // e.g., "lib/net10.0/FluentValidation.dll"
                        var fileName = Path.GetFileName(rel);
                        var full = Path.Combine(baseDir, fileName);
                        var types = _reflection.LoadAssemblyTypes(full);
                        info.Types.AddRange(types);
                    }

                    foreach (var child in pkgNode.Dependencies)
                    {
                        var c = BuildDependencyInfo(child, baseDir, framework);
                        if (c != null)
                            info.Children.Add(c);
                    }

                    return info;
                }
            case ProjectDependencyGraphNode pnode:
                {
                    // Currently unable to load assemblies of other projects
                    var info = new DependencyInfo
                    {
                        Name = pnode.Name,
                        NodeType = DependencyNodeTypes.Project,
                        Framework = framework
                    };

                    foreach (var child in pnode.Dependencies)
                    {
                        var c = BuildDependencyInfo(child, baseDir, framework);
                        if (c != null)
                            info.Children.Add(c);
                    }

                    return info;
                }
            default:
                {
                    var info = new DependencyInfo
                    {
                        Name = node.ToString() ?? "Unknown",
                        NodeType = DependencyNodeTypes.Unknown,
                        Framework = framework
                    };

                    foreach (var child in node.Dependencies)
                    {
                        var c = BuildDependencyInfo(child, baseDir, framework);
                        if (c != null)
                            info.Children.Add(c);
                    }

                    return info;
                }
        }
    }

    private static Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name);
        var requestingAssemblyLocation = args.RequestingAssembly?.Location;
        string? baseDirectory = null;

        if (!string.IsNullOrEmpty(requestingAssemblyLocation))
        {
            baseDirectory = Path.GetDirectoryName(requestingAssemblyLocation);
        }

        if (string.IsNullOrEmpty(baseDirectory))
        {
            // In single-file deployments Assembly.Location returns empty; fallback to app base directory
            baseDirectory = AppContext.BaseDirectory;
        }

        var assemblyPath = Path.Combine(baseDirectory, $"{assemblyName.Name}.dll");

        return File.Exists(assemblyPath)
            ? AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath)
            : null;
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
    }

    private static string? ExtractFrameworkFromRuntimeAssemblyPath(string runtimeAssemblyPath)
    {
        var parts = runtimeAssemblyPath.Split('/');
        if (parts.Length >= 2 && string.Equals(parts[0], "lib", StringComparison.OrdinalIgnoreCase))
        {
            return parts[1];
        }

        return null;
    }
}
