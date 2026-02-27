using System.Reflection;
using System.Xml.Linq;

namespace DotNetMetadataMcpServer;

/// <summary>
/// Parses .NET XML documentation files and provides lookup for member summaries.
/// XML doc files are generated alongside assemblies when &lt;GenerateDocumentationFile&gt; is enabled.
/// </summary>
public class XmlDocumentationProvider
{
    private readonly Dictionary<string, XElement> _members = new(StringComparer.Ordinal);

    private XmlDocumentationProvider()
    {
    }

    /// <summary>
    /// Tries to load an XML documentation file for the given assembly path.
    /// Returns null if the XML file does not exist or cannot be parsed.
    /// </summary>
    public static XmlDocumentationProvider? TryLoad(string assemblyPath, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(assemblyPath))
            return null;

        var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (!File.Exists(xmlPath))
            return null;

        try
        {
            var doc = XDocument.Load(xmlPath);
            var membersElement = doc.Root?.Element("members");
            if (membersElement == null)
                return null;

            var provider = new XmlDocumentationProvider();
            foreach (var member in membersElement.Elements("member"))
            {
                var name = member.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    provider._members[name] = member;
                }
            }

            logger?.LogDebug("Loaded XML documentation from {Path} with {Count} members", xmlPath, provider._members.Count);
            return provider;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to parse XML documentation file: {Path}", xmlPath);
            return null;
        }
    }

    /// <summary>
    /// Gets the summary for a type (e.g., "T:Namespace.ClassName").
    /// </summary>
    public string? GetTypeSummary(Type type)
    {
        var key = $"T:{GetTypeId(type)}";
        return GetSummaryText(key);
    }

    /// <summary>
    /// Gets the summary for a method.
    /// </summary>
    public string? GetMethodSummary(MethodInfo method)
    {
        var key = $"M:{GetMethodId(method)}";
        return GetSummaryText(key);
    }

    /// <summary>
    /// Gets the summary for a constructor.
    /// </summary>
    public string? GetConstructorSummary(ConstructorInfo constructor)
    {
        var key = $"M:{GetConstructorId(constructor)}";
        return GetSummaryText(key);
    }

    /// <summary>
    /// Gets the summary for a property.
    /// </summary>
    public string? GetPropertySummary(PropertyInfo property)
    {
        var key = $"P:{GetMemberDeclaringTypeId(property)}.{property.Name}";
        return GetSummaryText(key);
    }

    /// <summary>
    /// Gets the summary for a field.
    /// </summary>
    public string? GetFieldSummary(FieldInfo field)
    {
        var key = $"F:{GetMemberDeclaringTypeId(field)}.{field.Name}";
        return GetSummaryText(key);
    }

    /// <summary>
    /// Gets the summary for an event.
    /// </summary>
    public string? GetEventSummary(EventInfo eventInfo)
    {
        var key = $"E:{GetMemberDeclaringTypeId(eventInfo)}.{eventInfo.Name}";
        return GetSummaryText(key);
    }

    /// <summary>
    /// Gets the return value documentation for a method.
    /// </summary>
    public string? GetMethodReturns(MethodInfo method)
    {
        var key = $"M:{GetMethodId(method)}";
        return GetElementText(key, "returns");
    }

    /// <summary>
    /// Gets parameter documentation for a method.
    /// </summary>
    public Dictionary<string, string>? GetMethodParameters(MethodBase method)
    {
        var key = method is ConstructorInfo ctor
            ? $"M:{GetConstructorId(ctor)}"
            : $"M:{GetMethodId((MethodInfo)method)}";

        if (!_members.TryGetValue(key, out var element))
            return null;

        var paramElements = element.Elements("param").ToList();
        if (paramElements.Count == 0)
            return null;

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var param in paramElements)
        {
            var name = param.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(name))
            {
                result[name] = NormalizeWhitespace(param.Value);
            }
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Checks if this provider has any documentation loaded.
    /// </summary>
    public bool HasDocumentation => _members.Count > 0;

    /// <summary>
    /// Gets the total number of documented members.
    /// </summary>
    public int MemberCount => _members.Count;

    private string? GetSummaryText(string memberKey)
    {
        return GetElementText(memberKey, "summary");
    }

    private string? GetElementText(string memberKey, string elementName)
    {
        if (!_members.TryGetValue(memberKey, out var element))
            return null;

        var target = element.Element(elementName);
        if (target == null)
            return null;

        var text = NormalizeWhitespace(target.Value);
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    /// Generates the XML doc ID for a type (e.g., "Namespace.ClassName" or "Namespace.Outer+Inner" for nested types).
    /// </summary>
    internal static string GetTypeId(Type type)
    {
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            type = type.GetGenericTypeDefinition();
        }

        // Use FullName with + for nested types — XML docs use . instead of +
        var fullName = type.FullName ?? type.Name;
        // Replace + with . for nested types (XML docs convention)
        fullName = fullName.Replace('+', '.');

        // Strip generic arity suffixes for constructed generics, keep `` for definitions
        // Actually XML docs keep the `N suffix, so we DON'T strip it
        return fullName;
    }

    /// <summary>
    /// Generates the XML doc ID for a method (e.g., "Namespace.Class.Method(System.String,System.Int32)").
    /// </summary>
    internal static string GetMethodId(MethodInfo method)
    {
        var typeId = GetMemberDeclaringTypeId(method);
        var methodName = method.Name;

        // Handle explicit interface implementations
        methodName = methodName.Replace('.', '#');

        // Handle generic methods
        if (method.IsGenericMethod)
        {
            methodName += $"``{method.GetGenericArguments().Length}";
        }

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return $"{typeId}.{methodName}";

        var paramString = string.Join(",", parameters.Select(p => GetParameterTypeId(p.ParameterType)));
        return $"{typeId}.{methodName}({paramString})";
    }

    /// <summary>
    /// Generates the XML doc ID for a constructor.
    /// </summary>
    internal static string GetConstructorId(ConstructorInfo constructor)
    {
        var typeId = GetTypeId(constructor.DeclaringType!);
        var name = constructor.IsStatic ? "#cctor" : "#ctor";

        var parameters = constructor.GetParameters();
        if (parameters.Length == 0)
            return $"{typeId}.{name}";

        var paramString = string.Join(",", parameters.Select(p => GetParameterTypeId(p.ParameterType)));
        return $"{typeId}.{name}({paramString})";
    }

    private static string GetMemberDeclaringTypeId(MemberInfo member)
    {
        return member.DeclaringType != null ? GetTypeId(member.DeclaringType) : "";
    }

    /// <summary>
    /// Generates the XML doc parameter type ID.
    /// Handles by-ref, arrays, generics, nullable, etc.
    /// </summary>
    internal static string GetParameterTypeId(Type parameterType)
    {
        // Handle by-ref types (ref, out, in)
        if (parameterType.IsByRef)
        {
            return GetParameterTypeId(parameterType.GetElementType()!) + "@";
        }

        // Handle arrays
        if (parameterType.IsArray)
        {
            var elementType = GetParameterTypeId(parameterType.GetElementType()!);
            var rank = parameterType.GetArrayRank();
            return rank == 1
                ? $"{elementType}[]"
                : $"{elementType}[{new string(',', rank - 1)}]";
        }

        // Handle pointer types
        if (parameterType.IsPointer)
        {
            return GetParameterTypeId(parameterType.GetElementType()!) + "*";
        }

        // Handle generic types
        if (parameterType.IsGenericType)
        {
            var genericDef = parameterType.GetGenericTypeDefinition();
            // Nullable<T> is represented as System.Nullable{T} in XML docs
            var baseName = genericDef.FullName ?? genericDef.Name;
            // Remove the `N suffix
            var backtickIndex = baseName.IndexOf('`');
            if (backtickIndex > 0)
                baseName = baseName[..backtickIndex];

            var args = parameterType.GetGenericArguments();
            var argStrings = string.Join(",", args.Select(GetParameterTypeId));
            return $"{baseName}{{{argStrings}}}";
        }

        // Handle generic method type parameters (``0, ``1, etc.)
        if (parameterType.IsGenericParameter)
        {
            return parameterType.DeclaringMethod != null
                ? $"``{parameterType.GenericParameterPosition}"
                : $"`{parameterType.GenericParameterPosition}";
        }

        // Default: use FullName (which uses + for nested types)
        var name = parameterType.FullName ?? parameterType.Name;
        return name.Replace('+', '.');
    }

    /// <summary>
    /// Normalizes whitespace in XML doc text: trims lines, collapses multiple spaces.
    /// </summary>
    private static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        // Split by newlines, trim each line, rejoin with single space
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var trimmed = lines.Select(l => l.Trim()).Where(l => l.Length > 0);
        return string.Join(" ", trimmed);
    }
}
