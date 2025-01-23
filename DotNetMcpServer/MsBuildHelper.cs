using System;
using System.IO;
using Microsoft.Build.Evaluation;
//using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MySolution.ProjectScanner.Core;

public class MsBuildHelper
{
    private readonly ILogger<MsBuildHelper> _logger;

    public MsBuildHelper(ILogger<MsBuildHelper>? logger = null)
    {
        _logger = logger ?? NullLogger<MsBuildHelper>.Instance;
    }

    /// <summary>
    /// Считывает MSBuild-проектор (csproj) и возвращает 
    /// кортеж: (assemblyPath, assetsFilePath, targetFramework).
    /// </summary>
    public (string assemblyPath, string assetsFilePath, string targetFramework) 
        EvaluateProject(string csprojPath, string configuration = "Debug")
    {
        if (!File.Exists(csprojPath))
        {
            throw new FileNotFoundException("CSProj not found", csprojPath);
        }

        // Гарантированно регистрируем MSBuild (однократно за процесс)
        //MSBuildLocator.RegisterDefaults();

        _logger.LogInformation("Loading project {Path}", csprojPath);
        var project = new Project(csprojPath);

        // Устанавливаем конфигурацию (если нужно)
        // (Иногда нужно принудительно SetProperty("Configuration", configuration))
        project.SetProperty("Configuration", configuration);

        // Читаем необходимые свойства
        var assemblyName = project.GetPropertyValue("AssemblyName");
        var outputPath = project.GetPropertyValue("OutputPath")
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        var targetFramework = project.GetPropertyValue("TargetFramework");

        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            // Подстрахуемся и используем имя .csproj
            assemblyName = Path.GetFileNameWithoutExtension(csprojPath);
        }
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            // По умолчанию "bin/Debug"
            outputPath = Path.Combine("bin", configuration);
        }

        // Преобразуем в абсолютные пути
        var projectDir = Path.GetDirectoryName(Path.GetFullPath(csprojPath)) ?? "";
        var fullOutputPath = Path.GetFullPath(Path.Combine(projectDir, outputPath));

        // Cборка может иметь расширение .dll или .exe
        // Обычно .dll, но проверим оба варианта
        var possibleExtensions = new[] { ".dll", ".exe" };
        string? bestAssemblyPath = null;
        foreach (var ext in possibleExtensions)
        {
            var candidate = Path.Combine(fullOutputPath, assemblyName + ext);
            if (File.Exists(candidate))
            {
                bestAssemblyPath = candidate;
                break;
            }
        }
        if (bestAssemblyPath == null)
        {
            _logger.LogWarning("Assembly not found in {0} with name {1}", fullOutputPath, assemblyName);
            bestAssemblyPath = Path.Combine(fullOutputPath, assemblyName + ".dll"); // default assumption
        }

        // Путь к assets.json обычно: obj/{TargetFramework}/project.assets.json 
        // или просто obj/project.assets.json (зависит от NuGet версии).
        // Попробуем сначала TargetFramework-способ:
        var possibleObjDir1 = Path.Combine(projectDir, "obj", targetFramework ?? "");
        var possibleObjDir2 = Path.Combine(projectDir, "obj");
        var assetsFileName = "project.assets.json";

        var possibleAssets1 = Path.Combine(possibleObjDir1, assetsFileName);
        var possibleAssets2 = Path.Combine(possibleObjDir2, assetsFileName);

        string foundAssetsPath = "";
        if (File.Exists(possibleAssets1))
        {
            foundAssetsPath = possibleAssets1;
        }
        else if (File.Exists(possibleAssets2))
        {
            foundAssetsPath = possibleAssets2;
        }
        else
        {
            _logger.LogWarning("project.assets.json not found in obj folder(s).");
        }

        return (
            assemblyPath: bestAssemblyPath,
            assetsFilePath: foundAssetsPath,
            targetFramework: targetFramework ?? "netX"
        );
    }
}
