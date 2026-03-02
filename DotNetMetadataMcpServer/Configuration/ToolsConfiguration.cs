namespace DotNetMetadataMcpServer.Configuration;

public class ToolsConfiguration
{
    public const string SectionName = "Tools";

    public int DefaultPageSize { get; set; } = 20;
    public bool IndentResponse { get; set; } = true;
    public int AssemblyToolTimeoutSeconds { get; set; } = 30;
    public int NamespaceToolTimeoutSeconds { get; set; } = 30;
    public int TypeToolTimeoutSeconds { get; set; } = 30;
    public int TypeSearchToolTimeoutSeconds { get; set; } = 30;
    public int InheritanceToolTimeoutSeconds { get; set; } = 30;
    public int DependencyGraphToolTimeoutSeconds { get; set; } = 30;
    public int NuGetPackageSearchTimeoutSeconds { get; set; } = 45;
    public int NuGetPackageVersionsTimeoutSeconds { get; set; } = 60;
    public List<NuGetSourceConfiguration> NuGetSources { get; set; } =
    [
        new NuGetSourceConfiguration
        {
            Name = "nuget.org",
            Url = "https://api.nuget.org/v3/index.json",
            Enabled = true
        }
    ];
}

public class NuGetSourceConfiguration
{
    public required string Name { get; set; }
    public required string Url { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Comment { get; set; }
}
