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
    public List<TypeInfoModel> ProjectTypes { get; set; } = [];

    /// <summary>Dependency tree (packages, projects). For packages, types can also be viewed.</summary>
    public List<DependencyInfo> Dependencies { get; set; } = [];
}

/// <summary>Dependency node (package/project/...), plus types if loaded</summary>
public class DependencyInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string NodeType { get; set; } = "";  // "package", "project", "root", "tfm", ...
    public string? Framework { get; set; }

    public List<DependencyInfo> Children { get; set; } = [];

    /// <summary>Types from RuntimeAssemblies (if it is a package)</summary>
    public List<TypeInfoModel> Types { get; set; } = [];
}

/// <summary>Information about a single type</summary>
public class TypeInfoModel
{
    public string FullName { get; set; } = "";  // Considering generics (friendly name)
    public string? Documentation { get; set; }    // XML doc summary for the type
    public string? BaseType { get; set; }
    public List<string> Implements { get; set; } = [];
    public List<ConstructorInfoModel> Constructors { get; set; } = [];
    public List<MethodInfoModel> Methods { get; set; } = [];
    public List<PropertyInfoModel> Properties { get; set; } = [];
    public List<FieldInfoModel> Fields { get; set; } = [];
    public List<EventInfoModel> Events { get; set; } = [];
}

public class ConstructorInfoModel
{
    public string Name { get; set; } = "";
    public string? Documentation { get; set; }
    public List<string> ParameterTypes { get; set; } = [];
    public List<ParameterInfoModel> Parameters { get; set; } = [];
}

public class MethodInfoModel
{
    public string Name { get; set; } = "";
    public string ReturnType { get; set; } = "";
    public string? Documentation { get; set; }
    public string? ReturnsDocumentation { get; set; }
    public List<ParameterInfoModel> Parameters { get; set; } = [];
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public bool IsSealed { get; set; }
}

public class ParameterInfoModel
{
    public string Name { get; set; } = "";
    public string ParameterType { get; set; } = "";
    public string? Documentation { get; set; }
    public bool IsOptional { get; set; }
    public bool HasDefaultValue { get; set; }
    public string Modifier { get; set; } = ""; // "ref", "out", "in", "params"
}

public class PropertyInfoModel
{
    public string Name { get; set; } = "";
    public string PropertyType { get; set; } = "";
    public string? Documentation { get; set; }
    public bool HasPublicGetter { get; set; }
    public bool HasPublicSetter { get; set; }
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public bool IsSealed { get; set; }
    public bool IsRequired { get; set; }
    public bool IsInit { get; set; }  // init-only setter
}

public class FieldInfoModel
{
    public string Name { get; set; } = "";
    public string FieldType { get; set; } = "";
    public string? Documentation { get; set; }
    public bool IsStatic { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsConstant { get; set; }
    public bool IsRequired { get; set; }
}

public class EventInfoModel
{
    public string Name { get; set; } = "";
    public string EventHandlerType { get; set; } = "";
    public string? Documentation { get; set; }
    public bool IsStatic { get; set; }
}
