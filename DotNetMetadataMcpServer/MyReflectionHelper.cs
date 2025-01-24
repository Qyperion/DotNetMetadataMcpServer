using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetMetadataMcpServer;

public class MyReflectionHelper
{
    private readonly ILogger<MyReflectionHelper> _logger;
    private readonly HashSet<string> _loadedAssemblyPaths = new(StringComparer.OrdinalIgnoreCase);

    public MyReflectionHelper(ILogger<MyReflectionHelper>? logger = null)
    {
        _logger = logger ?? NullLogger<MyReflectionHelper>.Instance;
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
            // Сканируем только public классы/интерфейсы/enum и т.д.
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

            // Методы (только public DeclaredOnly)
            // Исключаем специальное имя (get_/set_/add_/remove_/op_ и т.д.)
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var m in methods)
            {
                if (m.IsSpecialName) // пропускаем get_XXX, set_XXX, add_XXX, remove_XXX etc.
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

            // Свойства (публичные хотя бы на геттере или сеттере)
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var p in props)
            {
                var getMethod = p.GetGetMethod(/*nonPublic*/ false);  // только публичный
                var setMethod = p.GetSetMethod(/*nonPublic*/ false);  // только публичный

                bool hasPublicGetter = (getMethod != null);
                bool hasPublicSetter = (setMethod != null);

                // Если нет ни публичного геттера, ни публичного сеттера - пропускаем
                if (!hasPublicGetter && !hasPublicSetter)
                    continue;

                var propModel = new PropertyInfoModel
                {
                    Name = p.Name,
                    PropertyType = GetFriendlyName(p.PropertyType),
                    HasPublicGetter = hasPublicGetter,
                    HasPublicSetter = hasPublicSetter,
                    // Статическое свойство, если get/set - static
                    IsStatic = (getMethod?.IsStatic ?? false) || (setMethod?.IsStatic ?? false)
                };
                ti.Properties.Add(propModel);
            }

            // Поля (только public)
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var f in fields)
            {
                if (!f.IsPublic) 
                    continue; // хотя GetFields(Public) уже исключает не публичные, но на всякий случай
                var fi = new FieldInfoModel
                {
                    Name = f.Name,
                    FieldType = GetFriendlyName(f.FieldType),
                    IsStatic = f.IsStatic
                };
                ti.Fields.Add(fi);
            }

            // События (public) 
            var events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var e in events)
            {
                // Если публичные методы add/remove
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

    /// <summary>Проверяем, является ли класс/структ/интерфейс полностью публичным (учитываем IsNestedPublic).</summary>
    private bool IsPublic(Type t)
    {
        return t.IsPublic || t.IsNestedPublic;
    }

    /// <summary>
    /// Формирует человекочитаемое имя типа (учитывая дженерики: List<int>, Dictionary<string, List<int>>).
    /// Без полного namespace, только короткое имя.
    /// Если нужно namespace — можно доработать.
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

        // Иначе просто t.Name (короткое имя).
        return t.Name;
    }
}
