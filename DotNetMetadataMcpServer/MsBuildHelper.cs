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
    /// Loads .csproj via MSBuild, retrieves OutputPath, AssemblyName, TargetFramework.
    /// Searches for the compiled assembly (dll or exe), as well as project.assets.json.
    /// </summary>
    public (string assemblyPath, string assetsFilePath, string targetFramework)
        EvaluateProject(string csprojPath, string configuration = "Debug")
    {
        if (!File.Exists(csprojPath))
            throw new FileNotFoundException("CSProj not found", csprojPath);

        _logger.LogInformation("Loading project: {Proj}", csprojPath);
        
        using var projectCollection = new ProjectCollection();

        // Unload any existing projects with the same path
        var existingProject = projectCollection.LoadedProjects
            .FirstOrDefault(p => string.Equals(p.FullPath, csprojPath, StringComparison.OrdinalIgnoreCase));
        if (existingProject != null)
        {
            projectCollection.UnloadProject(existingProject);
        }

        var project = new Project(csprojPath, null, null, projectCollection);
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

        // Search for project.assets.json
        // Usually: obj/{TargetFramework}/project.assets.json or obj/project.assets.json
        var assets1 = Path.Combine(projDir, "obj", targetFramework ?? "", "project.assets.json");
        var assets2 = Path.Combine(projDir, "obj", "project.assets.json");
        var assetsFile = File.Exists(assets1) ? assets1
                         : File.Exists(assets2) ? assets2
                         : "";

        if (string.IsNullOrEmpty(assetsFile))
        {
            _logger.LogWarning("project.assets.json not found in {0}", projDir);
        }
        
        projectCollection.UnloadAllProjects();

        return (finalAsmPath, assetsFile, targetFramework ?? "netX");
    }
}