using DependencyGraph.Core.Graph;
using DependencyGraph.Core.Graph.Factory;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.ProjectModel;
using NullLogger = NuGet.Common.NullLogger;

namespace DotNetMetadataMcpServer;

public class MyProjectScanner
{
    private readonly MsBuildHelper _msbuild;
    private readonly MyReflectionHelper _reflection;
    private readonly ILogger<MyProjectScanner> _logger;
    
    private readonly HashSet<IDependencyGraphNode> _visitedNodes = new();
    
    private string _baseDir = "";

    public MyProjectScanner(
        MsBuildHelper msBuildHelper,
        MyReflectionHelper reflectionHelper,
        ILogger<MyProjectScanner>? logger = null)
    {
        _msbuild = msBuildHelper;
        _reflection = reflectionHelper;
        _logger = logger ?? NullLogger<MyProjectScanner>.Instance;
    }

    /// <summary>
    /// Сканирует .csproj:
    /// 1. MSBuild → assemblyPath, assetsFilePath
    /// 2. Загружает публичные типы из самого проекта (assemblyPath)
    /// 3. Парсит project.assets.json, строит DependencyGraph
    /// 4. Для пакетов загружает сборки (через .RuntimeAssemblies)
    /// 5. Возвращает ProjectMetadata
    /// </summary>
    public ProjectMetadata ScanProject(string csprojPath)
    {
        MSBuildLocator.RegisterDefaults();
        
        var (asmPath, assetsPath, tfm) = _msbuild.EvaluateProject(csprojPath);

        var projectName = Path.GetFileNameWithoutExtension(csprojPath);
        var pm = new ProjectMetadata
        {
            ProjectName = projectName,
            TargetFramework = tfm,
            AssemblyPath = asmPath
        };

        // 1) Загрузить публичные типы самого проекта
        var projectTypes = _reflection.LoadAssemblyTypes(asmPath);
        pm.ProjectTypes.AddRange(projectTypes);

        // 2) Если нет assetsFile — пропускаем зависимости
        if (string.IsNullOrEmpty(assetsPath) || !File.Exists(assetsPath))
        {
            _logger.LogWarning("No project.assets.json found. Skip dependency scanning.");
            return pm;
        }

        // 3) Строим DependencyGraph
        var lockFileFormat = new LockFileFormat();
        var lockFile = lockFileFormat.Read(assetsPath, new NullLogger());

        var depGraphFactory = new DependencyGraphFactory(new DependencyGraphFactoryOptions
        {
            Excludes = ["Microsoft.*", "System.*"]
        });
        var graph = depGraphFactory.FromLockFile(lockFile);

        var rootNode = graph.RootNodes.FirstOrDefault() as RootProjectDependencyGraphNode;
        if (rootNode == null)
        {
            _logger.LogWarning("No RootProjectDependencyGraphNode found.");
            return pm;
        }

        var tfmNode = rootNode.Dependencies.OfType<TargetFrameworkDependencyGraphNode>().FirstOrDefault();
        if (tfmNode == null)
        {
            _logger.LogWarning("No TargetFrameworkDependencyGraphNode found under root.");
            return pm;
        }
        
        _baseDir = Path.GetDirectoryName(asmPath) ?? "";

        var depList = new List<DependencyInfo>();
        foreach (var child in tfmNode.Dependencies)
        {
            var d = BuildDependencyInfo(child);
            if (d != null) depList.Add(d);
        }
        pm.Dependencies = depList;

        return pm;
    }

    private DependencyInfo? BuildDependencyInfo(IDependencyGraphNode node)
    {
        // Проверяем, не заходили ли уже
        if (!_visitedNodes.Add(node)) 
            return null;

        switch (node)
        {
            case RootProjectDependencyGraphNode rootNode:
            {
                var info = new DependencyInfo
                {
                    Name = rootNode.Name,
                    NodeType = "root"
                };
                foreach (var child in rootNode.Dependencies)
                {
                    var c = BuildDependencyInfo(child);
                    if (c != null) info.Children.Add(c);
                }
                return info;
            }
            case TargetFrameworkDependencyGraphNode tfmNode:
            {
                var info = new DependencyInfo
                {
                    Name = tfmNode.ProjectName,
                    Version = tfmNode.TargetFrameworkIdentifier,
                    NodeType = "target framework dependency"
                };
                foreach (var child in tfmNode.Dependencies)
                {
                    var c = BuildDependencyInfo(child);
                    if (c != null) info.Children.Add(c);
                }
                return info;
            }
            case PackageDependencyGraphNode pkgNode:
            {
                var info = new DependencyInfo
                {
                    Name = pkgNode.Name,
                    Version = pkgNode.Version.ToNormalizedString(),
                    NodeType = "package"
                };
                // Грузим RuntimeAssemblies
                if (pkgNode.TargetLibrary != null)
                {
                    foreach (var asmItem in pkgNode.TargetLibrary.RuntimeAssemblies)
                    {
                        var rel = asmItem.Path; // например, "lib/net9.0/FluentValidation.dll"
                        var fileName = Path.GetFileName(rel);
                        var full = Path.Combine(_baseDir, fileName);
                        var types = _reflection.LoadAssemblyTypes(full);
                        info.Types.AddRange(types);
                    }
                }
                // Рекурсия
                foreach (var child in pkgNode.Dependencies)
                {
                    var c = BuildDependencyInfo(child);
                    if (c != null) info.Children.Add(c);
                }
                return info;
            }
            case ProjectDependencyGraphNode pnode:
            {
                // Пока не умеем загружать сборки других проектов
                var info = new DependencyInfo
                {
                    Name = pnode.Name,
                    NodeType = "project"
                };
                foreach (var child in pnode.Dependencies)
                {
                    var c = BuildDependencyInfo(child);
                    if (c != null) info.Children.Add(c);
                }
                return info;
            }
            default:
            {
                var info = new DependencyInfo
                {
                    Name = node.ToString() ?? "Unknown",
                    NodeType = "unknown"
                };
                foreach (var child in node.Dependencies)
                {
                    var c = BuildDependencyInfo(child);
                    if (c != null) info.Children.Add(c);
                }
                return info;
            }
        }
    }
}
