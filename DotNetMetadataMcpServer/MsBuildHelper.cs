using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetMetadataMcpServer;

public class MsBuildHelper
{
    private readonly ILogger<MsBuildHelper> _logger;

    public MsBuildHelper(ILogger<MsBuildHelper>? logger = null)
    {
        _logger = logger ?? NullLogger<MsBuildHelper>.Instance;
    }

    /// <summary>
    /// Загружает .csproj через MSBuild, берёт OutputPath, AssemblyName, TargetFramework.
    /// Ищет готовую сборку (dll или exe), а также project.assets.json.
    /// </summary>
    public (string assemblyPath, string assetsFilePath, string targetFramework)
        EvaluateProject(string csprojPath, string configuration = "Debug")
    {
        if (!File.Exists(csprojPath))
            throw new FileNotFoundException("CSProj not found", csprojPath);
        
        _logger.LogInformation("Loading project: {Proj}", csprojPath);
        var project = new Project(csprojPath);
        
        project.SetProperty("Configuration", configuration);

        var assemblyName = project.GetPropertyValue("AssemblyName");
        var outputPath = project.GetPropertyValue("OutputPath")
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        
        var targetFramework = project.GetPropertyValue("TargetFramework");

        if (string.IsNullOrWhiteSpace(assemblyName))
            assemblyName = Path.GetFileNameWithoutExtension(csprojPath);

        if (string.IsNullOrWhiteSpace(outputPath))
            outputPath = Path.Combine("bin", configuration);

        var projDir = Path.GetDirectoryName(Path.GetFullPath(csprojPath)) ?? "";
        var fullOutputPath = Path.GetFullPath(Path.Combine(projDir, outputPath));
        
        string? finalAsmPath = null;
        foreach (var ext in new[] { ".dll", ".exe" })
        {
            var candidate = Path.Combine(fullOutputPath, assemblyName + ext);
            if (File.Exists(candidate))
            {
                finalAsmPath = candidate;
                break;
            }
        }
        if (finalAsmPath == null)
        {
            finalAsmPath = Path.Combine(fullOutputPath, assemblyName + ".dll");
            _logger.LogWarning("Assembly not found, assume {0}", finalAsmPath);
        }

        // Ищем project.assets.json
        // Обычно: obj/{TargetFramework}/project.assets.json  или obj/project.assets.json
        var assets1 = Path.Combine(projDir, "obj", targetFramework ?? "", "project.assets.json");
        var assets2 = Path.Combine(projDir, "obj", "project.assets.json");
        var assetsFile = File.Exists(assets1) ? assets1 
                         : File.Exists(assets2) ? assets2 
                         : "";

        if (string.IsNullOrEmpty(assetsFile))
        {
            _logger.LogWarning("project.assets.json not found in {0}", projDir);
        }

        return (finalAsmPath, assetsFile, targetFramework ?? "netX");
    }
}
