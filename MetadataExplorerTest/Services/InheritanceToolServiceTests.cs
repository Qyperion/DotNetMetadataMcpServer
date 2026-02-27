using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Services;

namespace MetadataExplorerTest.Services;

[TestFixture]
[NonParallelizable]
public class InheritanceToolServiceTests
{
    private string _testProjectPath;
    private DependenciesScanner _scanner;
    private InheritanceToolService _service;

    [SetUp]
    public void Setup()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var relativePath = Path.Combine(testDirectory, "../../../../DotNetMetadataMcpServer/DotNetMetadataMcpServer.csproj");
        _testProjectPath = Path.GetFullPath(relativePath);
        _scanner = new DependenciesScanner(new MsBuildHelper(), new ReflectionTypesCollector());
        _service = new InheritanceToolService(_scanner, new ProjectMetadataCache());
    }

    [TearDown]
    public void TearDown()
    {
        _scanner.Dispose();
    }

    [Test]
    public void GetHierarchy_ForDerivedType_ReturnsBaseTypeChain()
    {
        var response = _service.GetHierarchy(_testProjectPath, "DotNetMetadataMcpServer.Models.TypeToolResponse");

        Assert.That(response.TypeFullName, Is.EqualTo("DotNetMetadataMcpServer.Models.TypeToolResponse"));
        Assert.That(response.DirectBaseType, Is.EqualTo("DotNetMetadataMcpServer.Models.Base.PagedResponse"));
        Assert.That(response.BaseTypeChain, Does.Contain("DotNetMetadataMcpServer.Models.Base.PagedResponse"));
    }

    [Test]
    public void GetHierarchy_ForBaseType_ReturnsDerivedTypes()
    {
        var response = _service.GetHierarchy(_testProjectPath, "DotNetMetadataMcpServer.Models.Base.PagedResponse");

        Assert.That(response.DerivedTypes, Is.Not.Empty);
        Assert.That(response.DerivedTypes.Any(t => t.Contains("TypeToolResponse", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(response.DerivedTypes.Any(t => t.Contains("NamespaceToolResponse", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(response.DerivedTypes.Any(t => t.Contains("AssemblyToolResponse", StringComparison.OrdinalIgnoreCase)), Is.True);
    }
}
