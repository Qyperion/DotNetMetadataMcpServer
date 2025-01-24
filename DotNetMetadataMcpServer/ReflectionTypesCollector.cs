using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetMetadataMcpServer;

public class ReflectionTypesCollector
{
    private readonly ILogger<ReflectionTypesCollector> _logger;
    private readonly HashSet<string> _loadedAssemblyPaths = new(StringComparer.OrdinalIgnoreCase);

    public ReflectionTypesCollector(ILogger<ReflectionTypesCollector>? logger = null)
    {
        _logger = logger ?? NullLogger<ReflectionTypesCollector>.Instance;
    }

    public List<TypeInfoModel> LoadAssemblyTypes(string asmPath)
    {
        var result = new List<TypeInfoModel>();
        if (string.IsNullOrEmpty(asmPath) || !File.Exists(asmPath))
        {
            _logger.LogDebug("Assembly not found: {Path}", asmPath);
            return result;
        }

        var fullPath = Path.GetFullPath(asmPath);
        if (!_loadedAssemblyPaths.Add(fullPath))
        {
            return result;
        }

        _logger.LogInformation("Loading assembly: {Path}", fullPath);

        Assembly asm;
        try
        {
            asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot load assembly {Path}", fullPath);
            return result;
        }

        Type[] allTypes;
        try
        {
            allTypes = asm.GetTypes();
        }
        catch (ReflectionTypeLoadException rtle)
        {
            allTypes = rtle.Types.Where(t => t != null).ToArray()!;
            _logger.LogWarning(rtle, "Some types could not be loaded from {Path}", fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving types from {Path}", fullPath);
            return result;
        }

        foreach (var type in allTypes)
        {
            // Scan only public classes/interfaces/enums, etc.
            if (!IsPublic(type))
                continue;
            if (type.FullName == null)
                continue;

            var ti = new TypeInfoModel
            {
                FullName = GetFriendlyName(type)
            };

            // constructors (public Instance/Static)
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var ctor in ctors)
            {
                var ctorModel = new ConstructorInfoModel
                {
                    Name = ctor.Name,
                    ParameterTypes = ctor.GetParameters().Select(p => GetFriendlyName(p.ParameterType)).ToList()
                };
                ti.Constructors.Add(ctorModel);
            }

            // Methods (only public DeclaredOnly)
            // Exclude special names (get_/set_/add_/remove_/op_, etc.)
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var m in methods)
            {
                if (m.IsSpecialName) // skip get_XXX, set_XXX, add_XXX, remove_XXX, etc.
                    continue;

                var mi = new MethodInfoModel
                {
                    Name = m.Name,
                    ReturnType = GetFriendlyName(m.ReturnType),
                    ParameterTypes = m.GetParameters().Select(p => GetFriendlyName(p.ParameterType)).ToList(),
                    IsStatic = m.IsStatic
                };
                ti.Methods.Add(mi);
            }

            // Properties (public at least on getter or setter)
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var p in props)
            {
                var getMethod = p.GetGetMethod(/*nonPublic*/ false);  // only public
                var setMethod = p.GetSetMethod(/*nonPublic*/ false);  // only public

                bool hasPublicGetter = (getMethod != null);
                bool hasPublicSetter = (setMethod != null);

                // If there is neither a public getter nor a public setter, skip
                if (!hasPublicGetter && !hasPublicSetter)
                    continue;

                var propModel = new PropertyInfoModel
                {
                    Name = p.Name,
                    PropertyType = GetFriendlyName(p.PropertyType),
                    HasPublicGetter = hasPublicGetter,
                    HasPublicSetter = hasPublicSetter,
                    // Static property if get/set is static
                    IsStatic = (getMethod?.IsStatic ?? false) || (setMethod?.IsStatic ?? false)
                };
                ti.Properties.Add(propModel);
            }

            // Fields (only public)
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var f in fields)
            {
                if (!f.IsPublic)
                    continue; // although GetFields(Public) already excludes non-public, just in case
                var fi = new FieldInfoModel
                {
                    Name = f.Name,
                    FieldType = GetFriendlyName(f.FieldType),
                    IsStatic = f.IsStatic
                };
                ti.Fields.Add(fi);
            }

            // Events (public)
            var events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var e in events)
            {
                // If public add/remove methods
                var addM = e.GetAddMethod(false);
                var removeM = e.GetRemoveMethod(false);
                if (addM == null && removeM == null)
                    continue;

                var ei = new EventInfoModel
                {
                    Name = e.Name,
                    EventHandlerType = GetFriendlyName(e.EventHandlerType),
                    IsStatic = (addM?.IsStatic ?? false) || (removeM?.IsStatic ?? false)
                };
                ti.Events.Add(ei);
            }

            result.Add(ti);
        }

        return result;
    }

    /// <summary>Check if the class/struct/interface is fully public (considering IsNestedPublic).</summary>
    private bool IsPublic(Type t)
    {
        return t.IsPublic || t.IsNestedPublic;
    }

    /// <summary>
    /// Generates a human-readable type name (considering generics: List<int>, Dictionary<string, List<int>>).
    /// Without full namespace, only short name.
    /// If namespace is needed, it can be improved.
    /// </summary>
    private string GetFriendlyName(Type t)
    {
        if (t.IsArray)
        {
            var elemName = GetFriendlyName(t.GetElementType()!);
            return elemName + "[]";
        }

        if (t.IsGenericType)
        {
            var name = t.Name;
            var backtick = name.IndexOf('`');
            if (backtick > 0)
            {
                name = name.Remove(backtick);
            }
            var args = t.GetGenericArguments().Select(a => GetFriendlyName(a)).ToArray();
            return $"{name}<{string.Join(", ", args)}>";
        }

        // Otherwise, just t.Name (short name).
        return t.Name;
    }
}