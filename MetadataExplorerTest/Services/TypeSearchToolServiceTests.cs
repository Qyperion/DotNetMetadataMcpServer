using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Services;

namespace MetadataExplorerTest.Services;

[TestFixture]
[NonParallelizable]
public class TypeSearchToolServiceTests
{
    private string _testProjectPath;
    private DependenciesScanner _scanner;
    private TypeSearchToolService _service;

    [SetUp]
    public void Setup()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var relativePath = Path.Combine(testDirectory, "../../../../DotNetMetadataMcpServer/DotNetMetadataMcpServer.csproj");
        _testProjectPath = Path.GetFullPath(relativePath);
        _scanner = new DependenciesScanner(new MsBuildHelper(), new ReflectionTypesCollector());
        _service = new TypeSearchToolService(_scanner, new ProjectMetadataCache());
    }

    [TearDown]
    public void TearDown()
    {
        _scanner.Dispose();
    }

    [Test]
    public void SearchTypes_WithTypeNameQuery_ReturnsMatchingTypes()
    {
        var response = _service.SearchTypes(_testProjectPath, "TypeToolService", [], [], "fullName", "asc", 1, 20);

        Assert.That(response.TypeMatches, Is.Not.Empty);
        Assert.That(response.TypeMatches.Any(t => t.FullName.Contains("TypeToolService", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(response.TotalItems, Is.GreaterThan(0));
        Assert.That(response.PageSize, Is.EqualTo(20));
    }

    [Test]
    public void SearchTypes_WithAssemblyFilter_RestrictsResults()
    {
        var response = _service.SearchTypes(
            _testProjectPath,
            "Response",
            ["DotNetMetadataMcpServer"],
            [],
            "fullName",
            "asc",
            1,
            50);

        Assert.That(response.TypeMatches, Is.Not.Empty);
        Assert.That(response.TypeMatches.All(t => t.AssemblyName.Equals("DotNetMetadataMcpServer", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(response.TotalItems, Is.GreaterThan(0));
        Assert.That(response.PageSize, Is.EqualTo(50));
    }

    [Test]
    public void SearchTypes_WithCaseSensitiveFilter_RespectsCaseSensitivity()
    {
        var sensitive = _service.SearchTypes(
            _testProjectPath,
            "typesearchtoolservice",
            [],
            [],
            true,
            "fullName",
            "asc",
            1,
            20);

        var insensitive = _service.SearchTypes(
            _testProjectPath,
            "typesearchtoolservice",
            [],
            [],
            false,
            "fullName",
            "asc",
            1,
            20);

        Assert.That(sensitive.TypeMatches, Is.Empty);
        Assert.That(insensitive.TypeMatches, Is.Not.Empty);
    }
}
