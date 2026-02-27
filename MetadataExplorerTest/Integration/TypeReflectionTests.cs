using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Services;

namespace MetadataExplorerTest.Integration;

[TestFixture]
[Parallelizable(ParallelScope.Self)] // Uses OneTimeSetUp with shared scanner
public class TypeReflectionTests
{
    private DependenciesScanner _scanner;
    private TypeToolService _service;
    private string _testProjectPath;

    [OneTimeSetUp]
    public void Setup()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        _testProjectPath = Path.GetFullPath(Path.Combine(testDirectory, "../../../../DotNetMetadataMcpServer/DotNetMetadataMcpServer.csproj"));
        _scanner = new DependenciesScanner(new MsBuildHelper(), new ReflectionTypesCollector());
        _service = new TypeToolService(_scanner, new ProjectMetadataCache());
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _scanner.Dispose();
    }

    [Test]
    public void GetTypes_ForSimpleTypeInfo_VerifyStructuredProperties()
    {
        var response = _service.GetTypes(_testProjectPath, [], ["*SimpleTypeInfo"], 1, 100);

        var typeInfo = response.TypeData.FirstOrDefault(t => t.FullName.EndsWith("SimpleTypeInfo"));
        Assert.That(typeInfo, Is.Not.Null, "SimpleTypeInfo type should be found");

        // Verify that properties are structured PropertyResponse objects
        Assert.That(typeInfo.Properties, Is.Not.Null);
        Assert.That(typeInfo.Properties, Is.Not.Empty, "SimpleTypeInfo should have properties");

        // Verify all properties have names and types
        Assert.That(typeInfo.Properties, Has.All.Matches<PropertyResponse>(p =>
            !string.IsNullOrEmpty(p.Name) && !string.IsNullOrEmpty(p.Type)));

        // Find FullName property
        var fullNameProp = typeInfo.Properties
            .FirstOrDefault(p => p.Name == "FullName");

        // Verify FullName property existence and structure
        Assert.Multiple(() =>
        {
            Assert.That(fullNameProp, Is.Not.Null, "Should have FullName property");
            Assert.That(fullNameProp!.Type, Does.Contain("String"));
            Assert.That(fullNameProp.HasGetter, Is.True);
            Assert.That(fullNameProp.IsRequired, Is.True);
        });
    }

    [Test]
    public void GetTypes_ForTypeWithInterfaces_IncludesImplementsList()
    {
        var response = _service.GetTypes(_testProjectPath, [], ["*Service*"], 1, 100);

        // Find a type that implements interfaces (e.g., IDisposable)
        var typeWithInterfaces = response.TypeData.FirstOrDefault(t =>
            t.Implements != null && t.Implements.Any());

        Assert.That(typeWithInterfaces, Is.Not.Null, "Should find at least one type implementing interfaces");
        Assert.That(typeWithInterfaces.Implements, Is.Not.Empty);
    }
}
