namespace DotNetMetadataMcpServer;

/// <summary>
/// Final structure returned to the MCP client
/// </summary>
public class ProjectMetadata
{
    public string ProjectName { get; set; } = "";
    public string TargetFramework { get; set; } = "";
    public string AssemblyPath { get; set; } = "";

    /// <summary>Public types (classes, interfaces, etc.) from the project itself</summary>
    public List<TypeInfoModel> ProjectTypes { get; set; } = new();

    /// <summary>Dependency tree (packages, projects). For packages, types can also be viewed.</summary>
    public List<DependencyInfo> Dependencies { get; set; } = new();
}

/// <summary>Dependency node (package/project/...), plus types if loaded</summary>
public class DependencyInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string NodeType { get; set; } = "";  // "package", "project", "root", "tfm", ...

    public List<DependencyInfo> Children { get; set; } = new();

    /// <summary>Types from RuntimeAssemblies (if it is a package)</summary>
    public List<TypeInfoModel> Types { get; set; } = new();
}

/// <summary>Information about a single type</summary>
public class TypeInfoModel
{
    public string FullName { get; set; } = "";  // Considering generics (friendly name)
    public List<ConstructorInfoModel> Constructors { get; set; } = new();
    public List<MethodInfoModel> Methods { get; set; } = new();
    public List<PropertyInfoModel> Properties { get; set; } = new();
    public List<FieldInfoModel> Fields { get; set; } = new();
    public List<EventInfoModel> Events { get; set; } = new();
}

public class ConstructorInfoModel
{
    public string Name { get; set; } = "";
    public List<string> ParameterTypes { get; set; } = new();
}

public class MethodInfoModel
{
    public string Name { get; set; } = "";
    public string ReturnType { get; set; } = "";
    public List<string> ParameterTypes { get; set; } = new();
    public bool IsStatic { get; set; }
}

public class PropertyInfoModel
{
    public string Name { get; set; } = "";
    public string PropertyType { get; set; } = "";
    public bool HasPublicGetter { get; set; }
    public bool HasPublicSetter { get; set; }
    public bool IsStatic { get; set; }
}

public class FieldInfoModel
{
    public string Name { get; set; } = "";
    public string FieldType { get; set; } = "";
    public bool IsStatic { get; set; }
}

public class EventInfoModel
{
    public string Name { get; set; } = "";
    public string EventHandlerType { get; set; } = "";
    public bool IsStatic { get; set; }
}