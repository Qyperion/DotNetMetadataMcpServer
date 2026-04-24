using DotNetMetadataMcpServer.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DotNetMetadataMcpServer.Models;

public class TypeToolResponse : PagedResponse
{
    public IEnumerable<SimpleTypeInfo> TypeData { get; set; } = [];
}

public class SimpleTypeInfo
{
    [Required]
    public required string FullName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Documentation { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Implements { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ConstructorResponse>? Constructors { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MethodResponse>? Methods { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PropertyResponse>? Properties { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FieldResponse>? Fields { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EventResponse>? Events { get; set; }
}

public class ParameterResponse
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Documentation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsOptional { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HasDefaultValue { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Modifier { get; init; }
}

public class ConstructorResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Documentation { get; init; }

    public List<ParameterResponse> Parameters { get; init; } = [];
}

public class MethodResponse
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string ReturnType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Documentation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReturnsDocumentation { get; init; }

    public List<ParameterResponse> Parameters { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsStatic { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsAbstract { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsVirtual { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsOverride { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsSealed { get; init; }
}

public class PropertyResponse
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Documentation { get; init; }

    public bool HasGetter { get; init; }

    public bool HasSetter { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsInit { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsStatic { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsAbstract { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsVirtual { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsOverride { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsSealed { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsRequired { get; init; }
}

public class FieldResponse
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Documentation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsStatic { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsReadOnly { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsConstant { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsRequired { get; init; }
}

public class EventResponse
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string HandlerType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Documentation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsStatic { get; init; }
}

public class TypeSearchToolResponse : PagedResponse
{
    public IEnumerable<TypeSearchMatch> TypeMatches { get; set; } = [];
}

public class TypeSearchMatch
{
    [Required]
    public required string FullName { get; init; }

    [Required]
    public required string AssemblyName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Documentation { get; init; }
}

public class InheritanceHierarchyResponse
{
    [Required]
    public required string TypeFullName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DirectBaseType { get; init; }

    public List<string> BaseTypeChain { get; init; } = [];

    public List<string> DerivedTypes { get; init; } = [];
}