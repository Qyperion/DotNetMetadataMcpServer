using DotNetMetadataMcpServer.Models;

namespace DotNetMetadataMcpServer.Helpers;

public static class TypeInfoModelMapper
{
    public static SimpleTypeInfo ToSimpleTypeInfo(TypeInfoModel model)
    {
        var result = new SimpleTypeInfo
        {
            FullName = model.FullName,
            Documentation = model.Documentation,
            BaseType = NullIfEmpty(model.BaseType)
        };

        if (model.Implements.Any())
            result.Implements = model.Implements;

        if (model.Constructors.Any())
            result.Constructors = model.Constructors.Select(MapConstructor).ToList();

        if (model.Methods.Any())
            result.Methods = model.Methods.Select(MapMethod).ToList();

        if (model.Properties.Any())
            result.Properties = model.Properties.Select(MapProperty).ToList();

        if (model.Fields.Any())
            result.Fields = model.Fields.Select(MapField).ToList();

        if (model.Events.Any())
            result.Events = model.Events.Select(MapEvent).ToList();

        return result;
    }

    private static ConstructorResponse MapConstructor(ConstructorInfoModel ctor)
    {
        return new ConstructorResponse
        {
            Documentation = NullIfEmpty(ctor.Documentation),
            Parameters = ctor.Parameters.Select(MapParameter).ToList()
        };
    }

    private static MethodResponse MapMethod(MethodInfoModel method)
    {
        return new MethodResponse
        {
            Name = method.Name,
            ReturnType = method.ReturnType,
            Documentation = NullIfEmpty(method.Documentation),
            ReturnsDocumentation = NullIfEmpty(method.ReturnsDocumentation),
            Parameters = method.Parameters.Select(MapParameter).ToList(),
            IsStatic = method.IsStatic,
            IsAbstract = method.IsAbstract,
            IsVirtual = method.IsVirtual,
            IsOverride = method.IsOverride,
            IsSealed = method.IsSealed
        };
    }

    private static PropertyResponse MapProperty(PropertyInfoModel prop)
    {
        return new PropertyResponse
        {
            Name = prop.Name,
            Type = prop.PropertyType,
            Documentation = NullIfEmpty(prop.Documentation),
            HasGetter = prop.HasPublicGetter,
            HasSetter = prop.HasPublicSetter,
            IsInit = prop.IsInit,
            IsStatic = prop.IsStatic,
            IsAbstract = prop.IsAbstract,
            IsVirtual = prop.IsVirtual,
            IsOverride = prop.IsOverride,
            IsSealed = prop.IsSealed,
            IsRequired = prop.IsRequired
        };
    }

    private static FieldResponse MapField(FieldInfoModel field)
    {
        return new FieldResponse
        {
            Name = field.Name,
            Type = field.FieldType,
            Documentation = NullIfEmpty(field.Documentation),
            IsStatic = field.IsStatic,
            IsReadOnly = field.IsReadOnly,
            IsConstant = field.IsConstant,
            IsRequired = field.IsRequired
        };
    }

    private static EventResponse MapEvent(EventInfoModel evt)
    {
        return new EventResponse
        {
            Name = evt.Name,
            HandlerType = evt.EventHandlerType,
            Documentation = NullIfEmpty(evt.Documentation),
            IsStatic = evt.IsStatic
        };
    }

    private static ParameterResponse MapParameter(ParameterInfoModel param)
    {
        return new ParameterResponse
        {
            Name = param.Name,
            Type = param.ParameterType,
            Documentation = NullIfEmpty(param.Documentation),
            IsOptional = param.IsOptional,
            HasDefaultValue = param.HasDefaultValue,
            Modifier = string.IsNullOrEmpty(param.Modifier) ? null : param.Modifier
        };
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrEmpty(value) ? null : value;
}
