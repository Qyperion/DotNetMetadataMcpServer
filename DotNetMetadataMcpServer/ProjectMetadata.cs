namespace DotNetMetadataMcpServer;

/// <summary>
/// Итоговая структура, отдаваемая клиенту MCP
/// </summary>
public class ProjectMetadata
{
    public string ProjectName { get; set; } = "";
    public string TargetFramework { get; set; } = "";
    public string AssemblyPath { get; set; } = "";

    /// <summary>Публичные типы (классы, интерфейсы и т.д.) из самого проекта</summary>
    public List<TypeInfoModel> ProjectTypes { get; set; } = new();

    /// <summary>Дерево зависимостей (пакеты, проекты). У пакетов — тоже можно смотреть типы.</summary>
    public List<DependencyInfo> Dependencies { get; set; } = new();
}

/// <summary>Узел зависимостей (package/project/...), плюс типы, если загружены</summary>
public class DependencyInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string NodeType { get; set; } = "";  // "package", "project", "root", "tfm", ...

    public List<DependencyInfo> Children { get; set; } = new();

    /// <summary>Типы из RuntimeAssemblies (если это пакет)</summary>
    public List<TypeInfoModel> Types { get; set; } = new();
}

/// <summary>Информация об одном типе</summary>
public class TypeInfoModel
{
    public string FullName { get; set; } = "";  // Учитывая дженерики (friendly name)
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
