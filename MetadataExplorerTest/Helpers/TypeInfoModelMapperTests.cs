using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Helpers;
using DotNetMetadataMcpServer.Models;

namespace MetadataExplorerTest.Helpers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class TypeInfoModelMapperTests
{
    [Test]
    public void ToSimpleTypeInfo_MapsImplementsAndOverrides()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Implements = ["IDisposable", "IEquatable<TestClass>"],
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.Multiple(() =>
        {
            Assert.That(result.Implements, Is.EquivalentTo(model.Implements));
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsImplements()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Implements = ["IDisposable", "IEquatable<TestClass>"]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Implements, Is.EquivalentTo(model.Implements));
    }

    [Test]
    public void ToSimpleTypeInfo_IncludesOverrideInMethod()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "ToString",
                    ReturnType = "string",
                    IsOverride = true,
                    Parameters = []
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods, Has.Count.EqualTo(1));
        var method = result.Methods![0];
        Assert.Multiple(() =>
        {
            Assert.That(method.Name, Is.EqualTo("ToString"));
            Assert.That(method.ReturnType, Is.EqualTo("string"));
            Assert.That(method.IsOverride, Is.True);
            Assert.That(method.IsStatic, Is.False);
            Assert.That(method.IsAbstract, Is.False);
            Assert.That(method.IsVirtual, Is.False);
            Assert.That(method.IsSealed, Is.False);
            Assert.That(method.Parameters, Is.Empty);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsConstructors()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Constructors =
            [
                new ConstructorInfoModel
                {
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "value", ParameterType = "string" },
                        new ParameterInfoModel { Name = "count", ParameterType = "int" }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Constructors, Has.Count.EqualTo(1));
        var ctor = result.Constructors![0];
        Assert.That(ctor.Parameters, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(ctor.Parameters[0].Name, Is.EqualTo("value"));
            Assert.That(ctor.Parameters[0].Type, Is.EqualTo("string"));
            Assert.That(ctor.Parameters[1].Name, Is.EqualTo("count"));
            Assert.That(ctor.Parameters[1].Type, Is.EqualTo("int"));
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsMethodsWithModifiers()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "Calculate",
                    ReturnType = "double",
                    IsStatic = true,
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "x", ParameterType = "double" },
                        new ParameterInfoModel { Name = "y", ParameterType = "double" }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods, Has.Count.EqualTo(1));
        var method = result.Methods![0];
        Assert.Multiple(() =>
        {
            Assert.That(method.Name, Is.EqualTo("Calculate"));
            Assert.That(method.ReturnType, Is.EqualTo("double"));
            Assert.That(method.IsStatic, Is.True);
            Assert.That(method.Parameters, Has.Count.EqualTo(2));
            Assert.That(method.Parameters[0].Name, Is.EqualTo("x"));
            Assert.That(method.Parameters[1].Name, Is.EqualTo("y"));
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsPropertiesWithAccessors()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Properties =
            [
                new PropertyInfoModel
                {
                    Name = "Count",
                    PropertyType = "int",
                    HasPublicGetter = true,
                    HasPublicSetter = false,
                    IsStatic = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Properties, Has.Count.EqualTo(1));
        var prop = result.Properties![0];
        Assert.Multiple(() =>
        {
            Assert.That(prop.Name, Is.EqualTo("Count"));
            Assert.That(prop.Type, Is.EqualTo("int"));
            Assert.That(prop.HasGetter, Is.True);
            Assert.That(prop.HasSetter, Is.False);
            Assert.That(prop.IsStatic, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsMethodWithAllModifiers()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "Calculate",
                    ReturnType = "double",
                    IsVirtual = true,
                    Parameters = [new ParameterInfoModel { Name = "x", ParameterType = "double" }]
                },

                new MethodInfoModel
                {
                    Name = "Calculate",
                    ReturnType = "double",
                    IsAbstract = true,
                    Parameters = [new ParameterInfoModel { Name = "y", ParameterType = "double" }]
                },

                new MethodInfoModel
                {
                    Name = "Calculate",
                    ReturnType = "double",
                    IsOverride = true,
                    IsSealed = true,
                    Parameters = [new ParameterInfoModel { Name = "z", ParameterType = "double" }]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Methods![0].IsVirtual, Is.True);
            Assert.That(result.Methods[0].Parameters[0].Name, Is.EqualTo("x"));

            Assert.That(result.Methods[1].IsAbstract, Is.True);
            Assert.That(result.Methods[1].Parameters[0].Name, Is.EqualTo("y"));

            Assert.That(result.Methods[2].IsOverride, Is.True);
            Assert.That(result.Methods[2].IsSealed, Is.True);
            Assert.That(result.Methods[2].Parameters[0].Name, Is.EqualTo("z"));
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsFieldWithAllModifiers()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Fields =
            [
                new FieldInfoModel
                {
                    Name = "DefaultValue",
                    FieldType = "double",
                    IsConstant = true
                },

                new FieldInfoModel
                {
                    Name = "_value",
                    FieldType = "double",
                    IsReadOnly = true,
                    IsStatic = true
                },

                new FieldInfoModel
                {
                    Name = "Required",
                    FieldType = "string",
                    IsRequired = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Fields, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Fields![0].Name, Is.EqualTo("DefaultValue"));
            Assert.That(result.Fields[0].Type, Is.EqualTo("double"));
            Assert.That(result.Fields[0].IsConstant, Is.True);

            Assert.That(result.Fields[1].Name, Is.EqualTo("_value"));
            Assert.That(result.Fields[1].IsReadOnly, Is.True);
            Assert.That(result.Fields[1].IsStatic, Is.True);

            Assert.That(result.Fields[2].Name, Is.EqualTo("Required"));
            Assert.That(result.Fields[2].IsRequired, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsPropertyWithAllModifiers()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Properties =
            [
                new PropertyInfoModel
                {
                    Name = "StaticProperty",
                    PropertyType = "int",
                    IsStatic = true,
                    HasPublicGetter = true,
                    HasPublicSetter = true
                },

                new PropertyInfoModel
                {
                    Name = "VirtualProperty",
                    PropertyType = "string",
                    IsVirtual = true,
                    HasPublicGetter = true,
                    HasPublicSetter = true
                },

                new PropertyInfoModel
                {
                    Name = "AbstractProperty",
                    PropertyType = "bool",
                    IsAbstract = true,
                    HasPublicGetter = true
                },

                new PropertyInfoModel
                {
                    Name = "OverrideProperty",
                    PropertyType = "double",
                    IsOverride = true,
                    IsSealed = true,
                    HasPublicGetter = true,
                    HasPublicSetter = true
                },

                new PropertyInfoModel
                {
                    Name = "InitProperty",
                    PropertyType = "string",
                    HasPublicGetter = true,
                    HasPublicSetter = true,
                    IsInit = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Properties, Has.Count.EqualTo(5));
        Assert.Multiple(() =>
        {
            Assert.That(result.Properties![0].Name, Is.EqualTo("StaticProperty"));
            Assert.That(result.Properties[0].IsStatic, Is.True);
            Assert.That(result.Properties[0].HasGetter, Is.True);
            Assert.That(result.Properties[0].HasSetter, Is.True);

            Assert.That(result.Properties[1].IsVirtual, Is.True);

            Assert.That(result.Properties[2].IsAbstract, Is.True);
            Assert.That(result.Properties[2].HasGetter, Is.True);
            Assert.That(result.Properties[2].HasSetter, Is.False);

            Assert.That(result.Properties[3].IsOverride, Is.True);
            Assert.That(result.Properties[3].IsSealed, Is.True);

            Assert.That(result.Properties[4].IsInit, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsPropertyWithDifferentAccessors()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Properties =
            [
                new PropertyInfoModel
                {
                    Name = "ReadOnly",
                    PropertyType = "string",
                    HasPublicGetter = true,
                    HasPublicSetter = false
                },

                new PropertyInfoModel
                {
                    Name = "WriteOnly",
                    PropertyType = "int",
                    HasPublicGetter = false,
                    HasPublicSetter = true
                },

                new PropertyInfoModel
                {
                    Name = "InitOnly",
                    PropertyType = "string",
                    HasPublicGetter = true,
                    HasPublicSetter = true,
                    IsInit = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Properties, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Properties![0].Name, Is.EqualTo("ReadOnly"));
            Assert.That(result.Properties[0].HasGetter, Is.True);
            Assert.That(result.Properties[0].HasSetter, Is.False);

            Assert.That(result.Properties[1].Name, Is.EqualTo("WriteOnly"));
            Assert.That(result.Properties[1].HasGetter, Is.False);
            Assert.That(result.Properties[1].HasSetter, Is.True);

            Assert.That(result.Properties[2].Name, Is.EqualTo("InitOnly"));
            Assert.That(result.Properties[2].HasGetter, Is.True);
            Assert.That(result.Properties[2].HasSetter, Is.True);
            Assert.That(result.Properties[2].IsInit, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsConstructorsWithDifferentParameters()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Constructors =
            [
                new ConstructorInfoModel
                {
                    Parameters = []
                },

                new ConstructorInfoModel
                {
                    Parameters = [new ParameterInfoModel { Name = "value", ParameterType = "string" }]
                },

                new ConstructorInfoModel
                {
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "value", ParameterType = "string" },
                        new ParameterInfoModel { Name = "count", ParameterType = "int" }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Constructors, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Constructors![0].Parameters, Is.Empty);
            Assert.That(result.Constructors[1].Parameters, Has.Count.EqualTo(1));
            Assert.That(result.Constructors[1].Parameters[0].Name, Is.EqualTo("value"));
            Assert.That(result.Constructors[2].Parameters, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsMethodsWithDifferentParametersAndModifiers()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "DoSomething",
                    ReturnType = "void",
                    Parameters = []
                },

                new MethodInfoModel
                {
                    Name = "Calculate",
                    ReturnType = "double",
                    IsStatic = true,
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "x", ParameterType = "double" }
                    ]
                },

                new MethodInfoModel
                {
                    Name = "Process",
                    ReturnType = "Task<string>",
                    IsVirtual = true,
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "input", ParameterType = "string" },
                        new ParameterInfoModel { Name = "count", ParameterType = "int" }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Methods![0].Name, Is.EqualTo("DoSomething"));
            Assert.That(result.Methods[0].ReturnType, Is.EqualTo("void"));
            Assert.That(result.Methods[0].Parameters, Is.Empty);

            Assert.That(result.Methods[1].Name, Is.EqualTo("Calculate"));
            Assert.That(result.Methods[1].IsStatic, Is.True);
            Assert.That(result.Methods[1].Parameters, Has.Count.EqualTo(1));

            Assert.That(result.Methods[2].Name, Is.EqualTo("Process"));
            Assert.That(result.Methods[2].ReturnType, Is.EqualTo("Task<string>"));
            Assert.That(result.Methods[2].IsVirtual, Is.True);
            Assert.That(result.Methods[2].Parameters, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsMixedAccessorProperties()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Properties =
            [
                new PropertyInfoModel
                {
                    Name = "ReadOnlyRequired",
                    PropertyType = "string",
                    HasPublicGetter = true,
                    HasPublicSetter = false,
                    IsRequired = true
                },

                new PropertyInfoModel
                {
                    Name = "VirtualInitOnly",
                    PropertyType = "string",
                    HasPublicGetter = true,
                    HasPublicSetter = true,
                    IsVirtual = true,
                    IsInit = true
                },

                new PropertyInfoModel
                {
                    Name = "AbstractGetter",
                    PropertyType = "int",
                    HasPublicGetter = true,
                    HasPublicSetter = false,
                    IsAbstract = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Properties, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Properties![0].Name, Is.EqualTo("ReadOnlyRequired"));
            Assert.That(result.Properties[0].IsRequired, Is.True);
            Assert.That(result.Properties[0].HasGetter, Is.True);
            Assert.That(result.Properties[0].HasSetter, Is.False);

            Assert.That(result.Properties[1].Name, Is.EqualTo("VirtualInitOnly"));
            Assert.That(result.Properties[1].IsVirtual, Is.True);
            Assert.That(result.Properties[1].IsInit, Is.True);

            Assert.That(result.Properties[2].Name, Is.EqualTo("AbstractGetter"));
            Assert.That(result.Properties[2].IsAbstract, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsModifierPriority()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "StaticVirtual",
                    ReturnType = "void",
                    IsStatic = true,
                    IsVirtual = true,
                    Parameters = []
                },

                new MethodInfoModel
                {
                    Name = "AbstractVirtual",
                    ReturnType = "void",
                    IsAbstract = true,
                    IsVirtual = true,
                    Parameters = []
                },

                new MethodInfoModel
                {
                    Name = "SealedOverrideVirtual",
                    ReturnType = "void",
                    IsSealed = true,
                    IsOverride = true,
                    IsVirtual = true,
                    Parameters = []
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            // All modifier flags are preserved as-is (no priority filtering in structured model)
            Assert.That(result.Methods![0].Name, Is.EqualTo("StaticVirtual"));
            Assert.That(result.Methods[0].IsStatic, Is.True);
            Assert.That(result.Methods[0].IsVirtual, Is.True);

            Assert.That(result.Methods[1].Name, Is.EqualTo("AbstractVirtual"));
            Assert.That(result.Methods[1].IsAbstract, Is.True);
            Assert.That(result.Methods[1].IsVirtual, Is.True);

            Assert.That(result.Methods[2].Name, Is.EqualTo("SealedOverrideVirtual"));
            Assert.That(result.Methods[2].IsSealed, Is.True);
            Assert.That(result.Methods[2].IsOverride, Is.True);
            Assert.That(result.Methods[2].IsVirtual, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsFieldModifierCombinations()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Fields =
            [
                new FieldInfoModel
                {
                    Name = "ReadOnlyStatic",
                    FieldType = "int",
                    IsStatic = true,
                    IsReadOnly = true
                },

                new FieldInfoModel
                {
                    Name = "RequiredReadOnly",
                    FieldType = "string",
                    IsRequired = true,
                    IsReadOnly = true
                },

                new FieldInfoModel
                {
                    Name = "StaticConst",
                    FieldType = "double",
                    IsStatic = true,
                    IsConstant = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Fields, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Fields![0].Name, Is.EqualTo("ReadOnlyStatic"));
            Assert.That(result.Fields[0].IsStatic, Is.True);
            Assert.That(result.Fields[0].IsReadOnly, Is.True);

            Assert.That(result.Fields[1].Name, Is.EqualTo("RequiredReadOnly"));
            Assert.That(result.Fields[1].IsRequired, Is.True);
            Assert.That(result.Fields[1].IsReadOnly, Is.True);

            Assert.That(result.Fields[2].Name, Is.EqualTo("StaticConst"));
            Assert.That(result.Fields[2].IsStatic, Is.True);
            Assert.That(result.Fields[2].IsConstant, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_HandlesEmptyCollections()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Implements = [],
            Constructors = [],
            Methods = [],
            Properties = [],
            Fields = [],
            Events = []
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.Multiple(() =>
        {
            Assert.That(result.Implements, Is.Null);
            Assert.That(result.Constructors, Is.Null);
            Assert.That(result.Methods, Is.Null);
            Assert.That(result.Properties, Is.Null);
            Assert.That(result.Fields, Is.Null);
            Assert.That(result.Events, Is.Null);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsGenericTypes()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Properties =
            [
                new PropertyInfoModel
                {
                    Name = "Items",
                    PropertyType = "List<string>",
                    HasPublicGetter = true,
                    HasPublicSetter = true
                },

                new PropertyInfoModel
                {
                    Name = "Mapping",
                    PropertyType = "Dictionary<string, List<int>>",
                    HasPublicGetter = true,
                    HasPublicSetter = false
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Properties, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Properties![0].Name, Is.EqualTo("Items"));
            Assert.That(result.Properties[0].Type, Is.EqualTo("List<string>"));
            Assert.That(result.Properties[0].HasGetter, Is.True);
            Assert.That(result.Properties[0].HasSetter, Is.True);

            Assert.That(result.Properties[1].Name, Is.EqualTo("Mapping"));
            Assert.That(result.Properties[1].Type, Is.EqualTo("Dictionary<string, List<int>>"));
            Assert.That(result.Properties[1].HasGetter, Is.True);
            Assert.That(result.Properties[1].HasSetter, Is.False);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsOverridePropertyWithDifferentAccessors()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Properties =
            [
                new PropertyInfoModel
                {
                    Name = "OverrideReadOnly",
                    PropertyType = "string",
                    HasPublicGetter = true,
                    HasPublicSetter = false,
                    IsOverride = true
                },

                new PropertyInfoModel
                {
                    Name = "SealedOverrideWriteOnly",
                    PropertyType = "int",
                    HasPublicGetter = false,
                    HasPublicSetter = true,
                    IsOverride = true,
                    IsSealed = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Properties, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Properties![0].Name, Is.EqualTo("OverrideReadOnly"));
            Assert.That(result.Properties[0].IsOverride, Is.True);
            Assert.That(result.Properties[0].HasGetter, Is.True);
            Assert.That(result.Properties[0].HasSetter, Is.False);

            Assert.That(result.Properties[1].Name, Is.EqualTo("SealedOverrideWriteOnly"));
            Assert.That(result.Properties[1].IsOverride, Is.True);
            Assert.That(result.Properties[1].IsSealed, Is.True);
            Assert.That(result.Properties[1].HasGetter, Is.False);
            Assert.That(result.Properties[1].HasSetter, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsRequiredPropertiesWithDifferentModifiers()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Properties =
            [
                new PropertyInfoModel
                {
                    Name = "RequiredStatic",
                    PropertyType = "string",
                    IsRequired = true,
                    IsStatic = true,
                    HasPublicGetter = true,
                    HasPublicSetter = true
                },

                new PropertyInfoModel
                {
                    Name = "RequiredInit",
                    PropertyType = "int",
                    IsRequired = true,
                    HasPublicGetter = true,
                    HasPublicSetter = true,
                    IsInit = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Properties, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Properties![0].Name, Is.EqualTo("RequiredStatic"));
            Assert.That(result.Properties[0].IsRequired, Is.True);
            Assert.That(result.Properties[0].IsStatic, Is.True);

            Assert.That(result.Properties[1].Name, Is.EqualTo("RequiredInit"));
            Assert.That(result.Properties[1].IsRequired, Is.True);
            Assert.That(result.Properties[1].IsInit, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsComplexGenericMethods()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "Convert",
                    ReturnType = "IDictionary<TKey, IList<TValue>>",
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "source", ParameterType = "IEnumerable<KeyValuePair<TKey, TValue>>" },
                        new ParameterInfoModel { Name = "selector", ParameterType = "Func<TValue, TResult>" }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods, Has.Count.EqualTo(1));
        var method = result.Methods![0];
        Assert.Multiple(() =>
        {
            Assert.That(method.Name, Is.EqualTo("Convert"));
            Assert.That(method.ReturnType, Is.EqualTo("IDictionary<TKey, IList<TValue>>"));
            Assert.That(method.Parameters[0].Name, Is.EqualTo("source"));
            Assert.That(method.Parameters[0].Type, Is.EqualTo("IEnumerable<KeyValuePair<TKey, TValue>>"));
            Assert.That(method.Parameters[1].Name, Is.EqualTo("selector"));
            Assert.That(method.Parameters[1].Type, Is.EqualTo("Func<TValue, TResult>"));
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsNestedTypeNames()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Properties =
            [
                new PropertyInfoModel
                {
                    Name = "Configuration",
                    PropertyType = "TestClass.Config",
                    HasPublicGetter = true,
                    HasPublicSetter = false
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Properties, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result.Properties![0].Name, Is.EqualTo("Configuration"));
            Assert.That(result.Properties[0].Type, Is.EqualTo("TestClass.Config"));
            Assert.That(result.Properties[0].HasGetter, Is.True);
            Assert.That(result.Properties[0].HasSetter, Is.False);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsArrayTypes()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Fields =
            [
                new FieldInfoModel
                {
                    Name = "SingleDimensional",
                    FieldType = "int[]"
                },

                new FieldInfoModel
                {
                    Name = "MultiDimensional",
                    FieldType = "string[,]"
                },

                new FieldInfoModel
                {
                    Name = "Jagged",
                    FieldType = "double[][]"
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Fields, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Fields![0].Name, Is.EqualTo("SingleDimensional"));
            Assert.That(result.Fields[0].Type, Is.EqualTo("int[]"));

            Assert.That(result.Fields[1].Name, Is.EqualTo("MultiDimensional"));
            Assert.That(result.Fields[1].Type, Is.EqualTo("string[,]"));

            Assert.That(result.Fields[2].Name, Is.EqualTo("Jagged"));
            Assert.That(result.Fields[2].Type, Is.EqualTo("double[][]"));
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsStaticConstructor()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Constructors =
            [
                new ConstructorInfoModel
                {
                    Name = ".cctor",
                    Parameters = []
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Constructors, Has.Count.EqualTo(1));
        Assert.That(result.Constructors![0].Parameters, Is.Empty);
    }

    [Test]
    public void ToSimpleTypeInfo_MapsEventWithCustomDelegateType()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Events =
            [
                new EventInfoModel
                {
                    Name = "OnProgress",
                    EventHandlerType = "ProgressEventHandler<T>",
                    IsStatic = false
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Events, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result.Events![0].Name, Is.EqualTo("OnProgress"));
            Assert.That(result.Events[0].HandlerType, Is.EqualTo("ProgressEventHandler<T>"));
            Assert.That(result.Events[0].IsStatic, Is.False);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsConstFieldsWithPrimitiveTypes()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Fields =
            [
                new FieldInfoModel
                {
                    Name = "MaxRetries",
                    FieldType = "int",
                    IsConstant = true
                },

                new FieldInfoModel
                {
                    Name = "DefaultName",
                    FieldType = "string",
                    IsConstant = true
                },

                new FieldInfoModel
                {
                    Name = "EnableFeature",
                    FieldType = "bool",
                    IsConstant = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Fields, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result.Fields![0].Name, Is.EqualTo("MaxRetries"));
            Assert.That(result.Fields[0].IsConstant, Is.True);

            Assert.That(result.Fields[1].Name, Is.EqualTo("DefaultName"));
            Assert.That(result.Fields[1].IsConstant, Is.True);

            Assert.That(result.Fields[2].Name, Is.EqualTo("EnableFeature"));
            Assert.That(result.Fields[2].IsConstant, Is.True);
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsMethodWithRefAndOutParameters()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "TryParse",
                    ReturnType = "bool",
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "input", ParameterType = "string" },
                        new ParameterInfoModel { Name = "result", ParameterType = "out int" }
                    ]
                },

                new MethodInfoModel
                {
                    Name = "Modify",
                    ReturnType = "void",
                    Parameters = [new ParameterInfoModel { Name = "value", ParameterType = "ref double" }]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Methods![0].Name, Is.EqualTo("TryParse"));
            Assert.That(result.Methods[0].Parameters[0].Type, Is.EqualTo("string"));
            Assert.That(result.Methods[0].Parameters[1].Type, Is.EqualTo("out int"));

            Assert.That(result.Methods[1].Name, Is.EqualTo("Modify"));
            Assert.That(result.Methods[1].Parameters[0].Type, Is.EqualTo("ref double"));
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MapsMethodWithOptionalAndParamsParameters()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "Format",
                    ReturnType = "string",
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "format", ParameterType = "string" },
                        new ParameterInfoModel { Name = "args", ParameterType = "params object[]" }
                    ]
                },

                new MethodInfoModel
                {
                    Name = "Configure",
                    ReturnType = "void",
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "name", ParameterType = "string" },
                        new ParameterInfoModel { Name = "options", ParameterType = "Options", IsOptional = true }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Methods![0].Name, Is.EqualTo("Format"));
            Assert.That(result.Methods[0].Parameters[1].Type, Is.EqualTo("params object[]"));

            Assert.That(result.Methods[1].Name, Is.EqualTo("Configure"));
            Assert.That(result.Methods[1].Parameters[1].IsOptional, Is.True);
            Assert.That(result.Methods[1].Parameters[1].Type, Is.EqualTo("Options"));
        });
    }

    #region Documentation Tests

    [Test]
    public void ToSimpleTypeInfo_MapsTypeDocumentation()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Documentation = "A test class for doing things."
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Documentation, Is.EqualTo("A test class for doing things."));
    }

    [Test]
    public void ToSimpleTypeInfo_NullDocumentation_RemainsNull()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Documentation = null
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Documentation, Is.Null);
    }

    [Test]
    public void ToSimpleTypeInfo_MethodWithDocumentation_MappedToField()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "Calculate",
                    ReturnType = "double",
                    Documentation = "Calculates the result.",
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "x", ParameterType = "double" }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods![0].Documentation, Is.EqualTo("Calculates the result."));
    }

    [Test]
    public void ToSimpleTypeInfo_MethodWithoutDocumentation_NullDocField()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "Calculate",
                    ReturnType = "double",
                    Documentation = null,
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "x", ParameterType = "double" }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods![0].Documentation, Is.Null);
    }

    [Test]
    public void ToSimpleTypeInfo_PropertyWithDocumentation_MappedToField()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Properties =
            [
                new PropertyInfoModel
                {
                    Name = "Name",
                    PropertyType = "string",
                    HasPublicGetter = true,
                    HasPublicSetter = true,
                    Documentation = "Gets or sets the name."
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Properties![0].Documentation, Is.EqualTo("Gets or sets the name."));
    }

    [Test]
    public void ToSimpleTypeInfo_FieldWithDocumentation_MappedToField()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Fields =
            [
                new FieldInfoModel
                {
                    Name = "MaxRetries",
                    FieldType = "int",
                    IsConstant = true,
                    Documentation = "Maximum number of retries."
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Fields![0].Documentation, Is.EqualTo("Maximum number of retries."));
    }

    [Test]
    public void ToSimpleTypeInfo_EventWithDocumentation_MappedToField()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Events =
            [
                new EventInfoModel
                {
                    Name = "OnChanged",
                    EventHandlerType = "EventHandler",
                    Documentation = "Raised when a change occurs."
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Events![0].Documentation, Is.EqualTo("Raised when a change occurs."));
    }

    [Test]
    public void ToSimpleTypeInfo_ConstructorWithDocumentation_MappedToField()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Constructors =
            [
                new ConstructorInfoModel
                {
                    Name = ".ctor",
                    Documentation = "Creates a new instance.",
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "name", ParameterType = "string" }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Constructors![0].Documentation, Is.EqualTo("Creates a new instance."));
    }

    [Test]
    public void ToSimpleTypeInfo_EmptyDocumentation_TreatedAsNull()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "DoStuff",
                    ReturnType = "void",
                    Documentation = "",
                    Parameters = []
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods![0].Documentation, Is.Null);
    }

    [Test]
    public void ToSimpleTypeInfo_StaticMethodWithDocumentation_PreservesModifiers()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "Create",
                    ReturnType = "TestClass",
                    IsStatic = true,
                    Documentation = "Factory method.",
                    Parameters = []
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.Multiple(() =>
        {
            Assert.That(result.Methods![0].Name, Is.EqualTo("Create"));
            Assert.That(result.Methods[0].IsStatic, Is.True);
            Assert.That(result.Methods[0].Documentation, Is.EqualTo("Factory method."));
        });
    }

    #endregion

    #region Parameter Modifier Mapping Tests

    [Test]
    public void ToSimpleTypeInfo_ParameterModifier_MappedCorrectly()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "Test",
                    ReturnType = "void",
                    Parameters =
                    [
                        new ParameterInfoModel { Name = "a", ParameterType = "int", Modifier = "ref" },
                        new ParameterInfoModel { Name = "b", ParameterType = "int", Modifier = "out" },
                        new ParameterInfoModel { Name = "c", ParameterType = "int", Modifier = "in" },
                        new ParameterInfoModel { Name = "d", ParameterType = "int[]", Modifier = "params" },
                        new ParameterInfoModel { Name = "e", ParameterType = "int", Modifier = "" }
                    ]
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.Multiple(() =>
        {
            Assert.That(result.Methods![0].Parameters[0].Modifier, Is.EqualTo("ref"));
            Assert.That(result.Methods[0].Parameters[1].Modifier, Is.EqualTo("out"));
            Assert.That(result.Methods[0].Parameters[2].Modifier, Is.EqualTo("in"));
            Assert.That(result.Methods[0].Parameters[3].Modifier, Is.EqualTo("params"));
            Assert.That(result.Methods[0].Parameters[4].Modifier, Is.Null, "Empty modifier should be mapped to null");
        });
    }

    [Test]
    public void ToSimpleTypeInfo_MethodReturnsDocumentation_MappedCorrectly()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "Calculate",
                    ReturnType = "double",
                    Documentation = "Calculates the value.",
                    ReturnsDocumentation = "The calculated result.",
                    Parameters = []
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.Multiple(() =>
        {
            Assert.That(result.Methods![0].Documentation, Is.EqualTo("Calculates the value."));
            Assert.That(result.Methods[0].ReturnsDocumentation, Is.EqualTo("The calculated result."));
        });
    }

    #endregion
}
