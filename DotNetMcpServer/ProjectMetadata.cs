namespace MySolution.ProjectScanner.Models;

/// <summary>
/// Итоговая структура, которую мы возвращаем клиенту MCP:
/// - Общая информация о проекте
/// - Дерево зависимостей
/// - Список (или дерево) типов, загруженных из сборки самого проекта
/// </summary>
public class ProjectMetadata
{
    public string ProjectName { get; set; } = "";
    public string TargetFramework { get; set; } = "";
    public string AssemblyPath { get; set; } = "";

    /// <summary>
    /// Информация о типах в самом проекте (отражение выходной сборки).
    /// </summary>
    public List<TypeInfoModel> ProjectTypes { get; set; } = new();

    /// <summary>
    /// Список зависимостей (пакеты/проекты).
    /// </summary>
    public List<DependencyInfo> Dependencies { get; set; } = new();
}

public class DependencyInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string NodeType { get; set; } = "";  // package, project, root, tfm, etc.

    // Рекурсивное дерево
    public List<DependencyInfo> Children { get; set; } = new();

    /// <summary>
    /// Типы, загруженные из RuntimeAssemblies (для пакетов).
    /// Для проектов по условию пока оставляем пустым.
    /// </summary>
    public List<TypeInfoModel> Types { get; set; } = new();
}

/// <summary>
/// Информация об одном типе (класс, интерфейс, ...).
/// Включаем конструкторы, методы, свойства, поля, события.
/// </summary>
public class TypeInfoModel
{
    public string FullName { get; set; } = "";
    public List<ConstructorInfoModel> Constructors { get; set; } = new();
    public List<MethodInfoModel> Methods { get; set; } = new();
    public List<PropertyInfoModel> Properties { get; set; } = new();
    public List<FieldInfoModel> Fields { get; set; } = new();
    public List<EventInfoModel> Events { get; set; } = new();
}

public class ConstructorInfoModel
{
    public string Name { get; set; } = "";
    public bool IsPublic { get; set; }
    public List<string> ParameterTypes { get; set; } = new();
}

public class MethodInfoModel
{
    public string Name { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool IsStatic { get; set; }
    public string ReturnType { get; set; } = "";
    public List<string> ParameterTypes { get; set; } = new();
}

public class PropertyInfoModel
{
    public string Name { get; set; } = "";
    public string PropertyType { get; set; } = "";
    public bool HasGetter { get; set; }
    public bool HasSetter { get; set; }
    public bool IsPublic { get; set; }
    public bool IsStatic { get; set; }
}

public class FieldInfoModel
{
    public string Name { get; set; } = "";
    public string FieldType { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool IsStatic { get; set; }
}

public class EventInfoModel
{
    public string Name { get; set; } = "";
    public string EventHandlerType { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool IsStatic { get; set; }
}
