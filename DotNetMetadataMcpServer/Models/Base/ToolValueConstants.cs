namespace DotNetMetadataMcpServer.Models.Base;

public static class DependencyNodeTypes
{
    public const string Root = "root";
    public const string TargetFramework = "target_framework";
    public const string Package = "package";
    public const string Project = "project";
    public const string Unknown = "unknown";
}

public static class DependencyGraphViewModes
{
    public const string Tree = "tree";
    public const string Flat = "flat";
}

public static class SortDirections
{
    public const string Asc = "asc";
    public const string Desc = "desc";
}

public static class TypeSearchSortFields
{
    public const string FullName = "fullName";
    public const string AssemblyName = "assemblyName";
}

public static class NuGetSearchSortFields
{
    public const string Relevance = "relevance";
    public const string Id = "id";
    public const string Version = "version";
    public const string Downloads = "downloads";
    public const string Published = "published";
}

public static class NuGetVersionSortFields
{
    public const string Relevance = "relevance";
    public const string Version = "version";
    public const string Downloads = "downloads";
    public const string Published = "published";
}
