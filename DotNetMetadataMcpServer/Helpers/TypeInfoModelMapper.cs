using DotNetMetadataMcpServer.Models;

namespace DotNetMetadataMcpServer.Helpers;

public static class TypeInfoModelMapper
{
    public static SimpleTypeInfo ToSimpleTypeInfo(TypeInfoModel model)
    {
        var result = new SimpleTypeInfo
        {
            FullName = model.FullName
        };

        if (model.Implements.Any())
            result.Implements = model.Implements;

        if (model.Constructors.Any())
            result.Constructors = model.Constructors.Select(FormatConstructor).ToList();
        
        if (model.Methods.Any())
            result.Methods = model.Methods.Select(FormatMethod).ToList();
        
        if (model.Properties.Any())
            result.Properties = model.Properties.Select(FormatProperty).ToList();
        
        if (model.Fields.Any())
            result.Fields = model.Fields.Select(FormatField).ToList();
        
        if (model.Events.Any())
            result.Events = model.Events.Select(FormatEvent).ToList();

        return result;
    }

    private static string FormatConstructor(ConstructorInfoModel ctor)
    {
        var parameters = string.Join(", ", ctor.Parameters.Select(p => $"{p.ParameterType} {p.Name}"));
        return $"({parameters})";
    }

    private static string FormatMethod(MethodInfoModel method)
    {
        var modifiers = new List<string>();
        
        // Order: static/abstract/virtual/sealed/override
        if (method.IsStatic) 
        {
            modifiers.Add("static");
        }
        else if (method.IsAbstract)
        {
            modifiers.Add("abstract");
        }
        else if (method.IsOverride)
        {
            if (method.IsSealed) modifiers.Add("sealed");
            modifiers.Add("override");
        }
        else if (method.IsVirtual)
        {
            modifiers.Add("virtual");
        }
        
        var modifierString = modifiers.Any() ? string.Join(" ", modifiers) + " " : "";
        var parameters = string.Join(", ", method.Parameters.Select(FormatParameter));
        return $"{modifierString}{method.ReturnType} {method.Name}({parameters})";
    }

    private static string FormatParameter(ParameterInfoModel param)
    {
        var prefix = !string.IsNullOrEmpty(param.Modifier) ? param.Modifier + " " : "";
        var nullableType = param.IsOptional && !param.ParameterType.EndsWith("?") 
            ? param.ParameterType + "?" 
            : param.ParameterType;
            
        // Always add "= null" for optional parameters
        var suffix = param.IsOptional ? " = null" : "";
        return $"{prefix}{nullableType} {param.Name}{suffix}";
    }

    private static string FormatProperty(PropertyInfoModel prop)
    {
        var modifiers = new List<string>();
        
        if (prop.IsStatic) modifiers.Add("static");
        else
        {
            if (prop.IsAbstract) modifiers.Add("abstract");
            else if (prop.IsVirtual) modifiers.Add("virtual");
            else if (prop.IsOverride)
            {
                if (prop.IsSealed) modifiers.Add("sealed");  // sealed comes before override
                modifiers.Add("override");
            }
        }
        
        if (prop.IsRequired) modifiers.Add("required");
        
        var modifierString = modifiers.Any() ? string.Join(" ", modifiers) + " " : "";
        var accessors = "";
        if (prop.HasPublicGetter && prop.HasPublicSetter) 
            accessors = prop.IsInit ? " { get; init; }" : " { get; set; }";
        else if (prop.HasPublicGetter) 
            accessors = " { get; }";
        else if (prop.HasPublicSetter) 
            accessors = prop.IsInit ? " { init; }" : " { set; }";
            
        return $"{modifierString}{prop.PropertyType} {prop.Name}{accessors}";
    }

    private static string FormatField(FieldInfoModel field)
    {
        var modifiers = new List<string>();
        
        // Order: static/const/required/readonly
        if (field.IsConstant)
        {
            modifiers.Add("const");
        }
        else
        {
            if (field.IsStatic) modifiers.Add("static");
            if (field.IsRequired) modifiers.Add("required");
            if (field.IsReadOnly) modifiers.Add("readonly");
        }
        
        var modifierString = modifiers.Any() ? string.Join(" ", modifiers) + " " : "";
        return $"{modifierString}{field.FieldType} {field.Name}";
    }

    private static string FormatEvent(EventInfoModel evt)
    {
        var staticModifier = evt.IsStatic ? "static " : "";
        return $"{staticModifier}event {evt.EventHandlerType} {evt.Name}";
    }
}
