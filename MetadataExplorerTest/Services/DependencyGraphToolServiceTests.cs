using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Services;

namespace MetadataExplorerTest.Services;

[TestFixture]
[NonParallelizable]
public class DependencyGraphToolServiceTests
{
    private string _testProjectPath;
    private DependenciesScanner _scanner;
    private DependencyGraphToolService _service;

    [SetUp]
    public void Setup()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var relativePath = Path.Combine(testDirectory, "../../../../DotNetMetadataMcpServer/DotNetMetadataMcpServer.csproj");
        _testProjectPath = Path.GetFullPath(relativePath);
        _scanner = new DependenciesScanner(new MsBuildHelper(), new ReflectionTypesCollector());
        _service = new DependencyGraphToolService(_scanner, new ProjectMetadataCache());
    }

    [TearDown]
    public void TearDown()
    {
        _scanner.Dispose();
    }

    [Test]
    public void GetDependencyGraph_ReturnsNonEmptyGraph()
    {
        var response = _service.GetDependencyGraph(_testProjectPath, [], [], 0, "tree");

        Assert.That(response.Dependencies, Is.Not.Empty);
        Assert.That(response.TotalNodes, Is.GreaterThan(0));
        Assert.That(response.ViewMode, Is.EqualTo("tree"));
        Assert.That(response.TotalItems, Is.GreaterThan(0));
        Assert.That(response.PageSize, Is.EqualTo(response.TotalItems));
    }

    [Test]
    public void GetDependencyGraph_WithFrameworkFilter_ReturnsFilteredGraph()
    {
        var response = _service.GetDependencyGraph(_testProjectPath, [], [], ["net10*"], [], 0, "tree");

        Assert.That(response, Is.Not.Null);
        Assert.That(response.ViewMode, Is.EqualTo("tree"));
    }
}
