using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MySolution.ProjectScanner.Models;

namespace MySolution.ProjectScanner.Core;

public class MyReflectionHelper
{
    private readonly ILogger<MyReflectionHelper> _logger;
    private readonly HashSet<string> _loadedAssemblyPaths = new(StringComparer.OrdinalIgnoreCase);

    public MyReflectionHelper(ILogger<MyReflectionHelper>? logger = null)
    {
        _logger = logger ?? NullLogger<MyReflectionHelper>.Instance;
    }

    /// <summary>
    /// Грузит сборку (dll/exe) и собирает информацию о типах.
    /// Если уже загружали такую же сборку, повторно не грузим.
    /// </summary>
    public List<TypeInfoModel> LoadAssemblyTypes(string assemblyFullPath)
    {
        var result = new List<TypeInfoModel>();
        if (string.IsNullOrEmpty(assemblyFullPath) || !File.Exists(assemblyFullPath))
        {
            _logger.LogDebug("Assembly not found: {Path}", assemblyFullPath);
            return result;
        }

        var normalizedPath = Path.GetFullPath(assemblyFullPath);
        if (!_loadedAssemblyPaths.Add(normalizedPath))
        {
            // уже грузили
            return result;
        }

        _logger.LogInformation("Loading assembly: {Path}", normalizedPath);

        Assembly asm;
        try
        {
            asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(normalizedPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot load assembly {Path}", normalizedPath);
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
            _logger.LogWarning(rtle, "Some types could not be loaded from {Path}", normalizedPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving types from {Path}", normalizedPath);
            return result;
        }

        foreach (var t in allTypes)
        {
            if (t == null || t.FullName == null)
                continue;

            var model = new TypeInfoModel { FullName = t.FullName };

            // Конструкторы
            var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var ctor in ctors)
            {
                var cModel = new ConstructorInfoModel
                {
                    Name = ctor.Name,
                    IsPublic = ctor.IsPublic,
                    ParameterTypes = ctor.GetParameters().Select(p => p.ParameterType.Name).ToList()
                };
                model.Constructors.Add(cModel);
            }

            // Методы
            var methods = t.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly
            );
            foreach (var m in methods)
            {
                var mModel = new MethodInfoModel
                {
                    Name = m.Name,
                    IsPublic = m.IsPublic,
                    IsStatic = m.IsStatic,
                    ReturnType = m.ReturnType.Name,
                    ParameterTypes = m.GetParameters().Select(p => p.ParameterType.Name).ToList()
                };
                model.Methods.Add(mModel);
            }

            // Свойства
            var props = t.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly
            );
            foreach (var p in props)
            {
                var pModel = new PropertyInfoModel
                {
                    Name = p.Name,
                    PropertyType = p.PropertyType.Name,
                    HasGetter = p.GetGetMethod(true) != null,
                    HasSetter = p.GetSetMethod(true) != null,
                    IsPublic = (p.GetMethod?.IsPublic ?? false) || (p.SetMethod?.IsPublic ?? false),
                    IsStatic = (p.GetMethod?.IsStatic ?? false) || (p.SetMethod?.IsStatic ?? false)
                };
                model.Properties.Add(pModel);
            }

            // Поля
            var fields = t.GetFields(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly
            );
            foreach (var f in fields)
            {
                var fModel = new FieldInfoModel
                {
                    Name = f.Name,
                    FieldType = f.FieldType.Name,
                    IsPublic = f.IsPublic,
                    IsStatic = f.IsStatic
                };
                model.Fields.Add(fModel);
            }

            // События
            var events = t.GetEvents(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly
            );
            foreach (var e in events)
            {
                var eModel = new EventInfoModel
                {
                    Name = e.Name,
                    EventHandlerType = e.EventHandlerType.Name,
                    IsPublic = (e.GetAddMethod()?.IsPublic ?? false) || (e.GetRemoveMethod()?.IsPublic ?? false),
                    IsStatic = (e.GetAddMethod()?.IsStatic ?? false) || (e.GetRemoveMethod()?.IsStatic ?? false)
                };
                model.Events.Add(eModel);
            }

            result.Add(model);
        }

        return result;
    }
}
