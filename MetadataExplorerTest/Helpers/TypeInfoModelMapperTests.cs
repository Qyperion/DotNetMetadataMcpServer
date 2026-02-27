using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Helpers;

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
    public void ToSimpleTypeInfo_IncludesOverrideInMethodFormat()
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

        Assert.That(result.Methods, Is.EqualTo(["override string ToString()"]));
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

        Assert.That(result.Constructors, Is.Not.Empty);
        Assert.That(result.Constructors, Has.All.Matches<string>(s => s.StartsWith("(") && s.EndsWith(")")));
        Assert.That(result.Constructors, Is.EqualTo(["(string value, int count)"]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsMethodsWithModifiers()
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

        Assert.That(result.Methods, Is.EqualTo(["static double Calculate(double x, double y)"]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsPropertiesWithAccessors()
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

        Assert.That(result.Properties, Is.EqualTo(["static int Count { get; }"]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsMethodWithAllModifiers()
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

        Assert.That(result.Methods, Is.Not.Empty);
        Assert.That(result.Methods, Has.Some.Contains("virtual"));
        Assert.That(result.Methods, Has.Some.Contains("abstract"));
        Assert.That(result.Methods, Has.Some.Contains("sealed override"));
        Assert.That(result.Methods, Is.EquivalentTo([
            "virtual double Calculate(double x)",
            "abstract double Calculate(double y)",
            "sealed override double Calculate(double z)"  // Updated order to match C# convention
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsFieldWithAllModifiers()
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

        Assert.That(result.Fields, Is.EquivalentTo([
            "const double DefaultValue",
            "static readonly double _value",
            "required string Required"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsPropertyWithAllModifiers()
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

        Assert.That(result.Properties, Is.EquivalentTo([
            "static int StaticProperty { get; set; }",
            "virtual string VirtualProperty { get; set; }",
            "abstract bool AbstractProperty { get; }",
            "sealed override double OverrideProperty { get; set; }",
            "string InitProperty { get; init; }"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsPropertyWithDifferentAccessors()
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

        Assert.That(result.Properties, Is.EquivalentTo([
            "string ReadOnly { get; }",
            "int WriteOnly { set; }",
            "string InitOnly { get; init; }"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsConstructorsWithDifferentParameters()
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

        Assert.That(result.Constructors, Is.EquivalentTo([
            "()",
            "(string value)",
            "(string value, int count)"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsMethodsWithDifferentParametersAndModifiers()
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

        Assert.That(result.Methods, Is.EquivalentTo([
            "void DoSomething()",
            "static double Calculate(double x)",
            "virtual Task<string> Process(string input, int count)"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsMixedAccessorProperties()
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

        Assert.That(result.Properties, Is.EquivalentTo([
            "required string ReadOnlyRequired { get; }",
            "virtual string VirtualInitOnly { get; init; }",
            "abstract int AbstractGetter { get; }"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsOrderOfModifiers()
    {
        var model = new TypeInfoModel
        {
            FullName = "TestNamespace.TestClass",
            Methods =
            [
                new MethodInfoModel
                {
                    Name = "StaticVirtual", // static wins over virtual
                    ReturnType = "void",
                    IsStatic = true,
                    IsVirtual = true,
                    Parameters = []
                },

                new MethodInfoModel
                {
                    Name = "AbstractVirtual", // abstract wins over virtual
                    ReturnType = "void",
                    IsAbstract = true,
                    IsVirtual = true,
                    Parameters = []
                },

                new MethodInfoModel
                {
                    Name = "SealedOverrideVirtual", // sealed override wins over virtual
                    ReturnType = "void",
                    IsSealed = true,
                    IsOverride = true,
                    IsVirtual = true,
                    Parameters = []
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Methods, Is.EquivalentTo([
            "static void StaticVirtual()",
            "abstract void AbstractVirtual()",
            "sealed override void SealedOverrideVirtual()"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsFieldModifierCombinations()
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
                    Name = "StaticConst", // const implies static
                    FieldType = "double",
                    IsStatic = true,
                    IsConstant = true
                }
            ]
        };

        var result = TypeInfoModelMapper.ToSimpleTypeInfo(model);

        Assert.That(result.Fields, Is.EquivalentTo([
            "static readonly int ReadOnlyStatic",
            "required readonly string RequiredReadOnly",
            "const double StaticConst"
        ]));
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
    public void ToSimpleTypeInfo_HandlesGenericTypes()
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

        Assert.That(result.Properties, Is.EquivalentTo([
            "List<string> Items { get; set; }",
            "Dictionary<string, List<int>> Mapping { get; }"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsOverridePropertyWithDifferentAccessors()
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

        Assert.That(result.Properties, Is.EquivalentTo([
            "override string OverrideReadOnly { get; }",
            "sealed override int SealedOverrideWriteOnly { set; }"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsRequiredPropertiesWithDifferentModifiers()
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

        Assert.That(result.Properties, Is.EquivalentTo([
            "static required string RequiredStatic { get; set; }",
            "required int RequiredInit { get; init; }"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsComplexGenericMethods()
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

        Assert.That(result.Methods, Is.EqualTo([
            "IDictionary<TKey, IList<TValue>> Convert(IEnumerable<KeyValuePair<TKey, TValue>> source, Func<TValue, TResult> selector)"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsNestedTypeNames()
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

        Assert.That(result.Properties, Is.EqualTo(["TestClass.Config Configuration { get; }"]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsArrayTypes()
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

        Assert.That(result.Fields, Is.EquivalentTo([
            "int[] SingleDimensional",
            "string[,] MultiDimensional",
            "double[][] Jagged"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsStaticConstructor()
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

        Assert.That(result.Constructors, Is.EqualTo(["()"]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsEventWithCustomDelegateType()
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

        Assert.That(result.Events, Is.EqualTo(["event ProgressEventHandler<T> OnProgress"]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsConstFieldsWithPrimitiveTypes()
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

        Assert.That(result.Fields, Is.EquivalentTo([
            "const int MaxRetries",
            "const string DefaultName",
            "const bool EnableFeature"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsMethodWithRefAndOutParameters()
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

        Assert.That(result.Methods, Is.EquivalentTo([
            "bool TryParse(string input, out int result)",
            "void Modify(ref double value)"
        ]));
    }

    [Test]
    public void ToSimpleTypeInfo_FormatsMethodWithOptionalAndParamsParameters()
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

        Assert.That(result.Methods, Is.EquivalentTo([
            "string Format(string format, params object[] args)",
            "void Configure(string name, Options? options = null)"
        ]));
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
    public void ToSimpleTypeInfo_MethodWithDocumentation_AppendsEmDash()
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

        Assert.That(result.Methods, Has.Exactly(1).EqualTo(
            "double Calculate(double x) \u2014 Calculates the result."));
    }

    [Test]
    public void ToSimpleTypeInfo_MethodWithoutDocumentation_NoEmDash()
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

        Assert.That(result.Methods, Has.Exactly(1).EqualTo("double Calculate(double x)"));
    }

    [Test]
    public void ToSimpleTypeInfo_PropertyWithDocumentation_AppendsEmDash()
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

        Assert.That(result.Properties, Has.Exactly(1).EqualTo(
            "string Name { get; set; } \u2014 Gets or sets the name."));
    }

    [Test]
    public void ToSimpleTypeInfo_FieldWithDocumentation_AppendsEmDash()
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

        Assert.That(result.Fields, Has.Exactly(1).EqualTo(
            "const int MaxRetries \u2014 Maximum number of retries."));
    }

    [Test]
    public void ToSimpleTypeInfo_EventWithDocumentation_AppendsEmDash()
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

        Assert.That(result.Events, Has.Exactly(1).EqualTo(
            "event EventHandler OnChanged \u2014 Raised when a change occurs."));
    }

    [Test]
    public void ToSimpleTypeInfo_ConstructorWithDocumentation_AppendsEmDash()
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

        Assert.That(result.Constructors, Has.Exactly(1).EqualTo(
            "(string name) \u2014 Creates a new instance."));
    }

    [Test]
    public void ToSimpleTypeInfo_EmptyDocumentation_TreatedAsNoDoc()
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

        Assert.That(result.Methods, Has.Exactly(1).EqualTo("void DoStuff()"));
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

        Assert.That(result.Methods, Has.Exactly(1).EqualTo(
            "static TestClass Create() \u2014 Factory method."));
    }

    #endregion
}
