using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NuGet.ProjectModel;
using DependencyGraph.Core;
using DependencyGraph.Core.Graph;
using DependencyGraph.Core.Graph.Factory;
using MySolution.ProjectScanner.Models;
using NullLogger = NuGet.Common.NullLogger;

namespace MySolution.ProjectScanner.Core;

public class MyProjectScanner
{
    private readonly MsBuildHelper _msbuild;
    private readonly MyReflectionHelper _reflection;
    private readonly ILogger<MyProjectScanner> _logger;

    // Чтобы не обходить один и тот же нод несколько раз
    private readonly HashSet<IDependencyGraphNode> _visitedNodes = new();

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
    /// Полный цикл:
    /// 1) MSBuild: узнать output assembly, assetsFile
    /// 2) Загрузить основные типы проекта
    /// 3) Прочитать DependencyGraph, обойти пакеты (и пр.).
    /// 4) Вернуть ProjectMetadata.
    /// </summary>
    public ProjectMetadata ScanProject(string csprojPath)
    {
        // 1) MSBuild 
        var (asmPath, assetsPath, tfm) = _msbuild.EvaluateProject(csprojPath);

        var projectName = Path.GetFileNameWithoutExtension(csprojPath);
        var metadata = new ProjectMetadata
        {
            ProjectName = projectName,
            TargetFramework = tfm,
            AssemblyPath = asmPath
        };

        // 2) Рефлектим саму сборку проекта
        var mainProjectTypes = _reflection.LoadAssemblyTypes(asmPath);
        metadata.ProjectTypes.AddRange(mainProjectTypes);

        // 3) Сканируем зависимости через project.assets.json
        if (!File.Exists(assetsPath))
        {
            _logger.LogWarning("No project.assets.json found at {0}; skipping dependency scan", assetsPath);
            return metadata; // Вернём хоть какие-то данные
        }

        var lockFileFormat = new LockFileFormat();
        var lockFile = lockFileFormat.Read(filePath: assetsPath, log: new NullLogger());

        var depGraphFactory = new DependencyGraphFactory(new DependencyGraphFactoryOptions
        {
            // Исключаем Microsoft.* / System.* (пример)
            Excludes = new[] { "Microsoft.*", "System.*" }
        });
        var graph = depGraphFactory.FromLockFile(lockFile);

        // Обычно 1 rootNode
        var rootNode = graph.RootNodes.FirstOrDefault() as RootProjectDependencyGraphNode;
        if (rootNode == null)
        {
            _logger.LogWarning("No RootProjectDependencyGraphNode in the graph. Possibly empty?");
            return metadata;
        }

        // Находим TFM-node (одна)
        var tfmNode = rootNode.Dependencies.OfType<TargetFrameworkDependencyGraphNode>().FirstOrDefault();
        if (tfmNode == null)
        {
            _logger.LogWarning("No TargetFrameworkDependencyGraphNode under root node?");
            return metadata;
        }

        // Обходим рекурсивно
        var depInfos = new List<DependencyInfo>();
        foreach (var child in tfmNode.Dependencies)
        {
            var d = BuildDependencyInfo(child, Path.GetDirectoryName(assetsPath) ?? "");
            if (d != null) depInfos.Add(d);
        }
        metadata.Dependencies = depInfos;

        return metadata;
    }

    private DependencyInfo? BuildDependencyInfo(IDependencyGraphNode node, string baseDir)
    {
        if (!_visitedNodes.Add(node))
            return null; // уже были

        switch (node)
        {
            case RootProjectDependencyGraphNode rootNode:
            {
                var info = new DependencyInfo
                {
                    Name = rootNode.Name,
                    Version = "",
                    NodeType = "root"
                };
                foreach (var child in rootNode.Dependencies)
                {
                    var c = BuildDependencyInfo(child, baseDir);
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
                    NodeType = "tfm"
                };
                foreach (var child in tfmNode.Dependencies)
                {
                    var c = BuildDependencyInfo(child, baseDir);
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
                // Грузим dll-ки (RuntimeAssemblies)
                if (pkgNode.TargetLibrary != null)
                {
                    foreach (var asmItem in pkgNode.TargetLibrary.RuntimeAssemblies)
                    {
                        var asmRelPath = asmItem.Path;
                        var asmFullPath = Path.Combine(baseDir, asmRelPath);
                        var types = _reflection.LoadAssemblyTypes(asmFullPath);
                        info.Types.AddRange(types);
                    }
                }
                // Рекурсивно обходим Dependencies
                foreach (var child in pkgNode.Dependencies)
                {
                    var c = BuildDependencyInfo(child, baseDir);
                    if (c != null) info.Children.Add(c);
                }
                return info;
            }
            case ProjectDependencyGraphNode prjNode:
            {
                // Пока не умеем искать dll другой проект, 
                // так что просто собираем дерево, но без типов.
                var info = new DependencyInfo
                {
                    Name = prjNode.Name,
                    Version = "",
                    NodeType = "project"
                };
                foreach (var child in prjNode.Dependencies)
                {
                    var c = BuildDependencyInfo(child, baseDir);
                    if (c != null) info.Children.Add(c);
                }
                return info;
            }
            default:
            {
                // fallback
                var info = new DependencyInfo
                {
                    Name = node.ToString() ?? "UnknownNode",
                    NodeType = "unknown"
                };
                foreach (var child in node.Dependencies)
                {
                    var c = BuildDependencyInfo(child, baseDir);
                    if (c != null) info.Children.Add(c);
                }
                return info;
            }
        }
    }
}
