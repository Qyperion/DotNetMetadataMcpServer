using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Services;
using Moq;

namespace MetadataExplorerTest;

[TestFixture]
public class ServicesIntegrationTests
{
    private string _testProjectPath;
    private DependenciesScanner _scanner;
    private static List<string> _foundAssemblies;
    private static List<string> _foundNamespaces;

    [SetUp]
    public void Setup()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var relativePath = Path.Combine(testDirectory, "../../../../DotNetMetadataMcpServer/DotNetMetadataMcpServer.csproj");
        _testProjectPath = Path.GetFullPath(relativePath);
        
        if (!File.Exists(_testProjectPath))
            Assert.Inconclusive("Test project file not found: " + _testProjectPath);
        
        _scanner = new DependenciesScanner(new MsBuildHelper(), new ReflectionTypesCollector());
    }
    
    [TearDown]
    public void TearDown()
    {
        _scanner.Dispose();
    }
    
    [Test, Order(1)]
    public void AssemblyToolService_ShouldReturnAssemblies()
    {
        var service = new AssemblyToolService(_scanner);
        var response = service.GetAssemblies(_testProjectPath, new List<string>(), 1, 50);

        Assert.That(response.AssemblyNames, Is.Not.Null);
        Assert.That(response.AssemblyNames.Count, Is.GreaterThan(0));
        
        _foundAssemblies = response.AssemblyNames.Take(2).ToList();
    }

    [Test, Order(2)]
    public void NamespaceToolService_ShouldReturnNamespacesFromFoundAssemblies()
    {
        Assert.That(_foundAssemblies, Is.Not.Null, "Previous test must be run first");
        
        var service = new NamespaceToolService(_scanner);
        var response = service.GetNamespaces(_testProjectPath, _foundAssemblies, new List<string>(), 1, 50);

        Assert.That(response.Namespaces, Is.Not.Null);
        Assert.That(response.Namespaces.Count, Is.GreaterThan(0));
        
        _foundNamespaces = response.Namespaces.Take(1).Concat(response.Namespaces.TakeLast(1)).ToList(); 
    }

    [Test, Order(3)]
    public void TypeToolService_ShouldReturnTypesFromFoundNamespaces()
    {
        Assert.That(_foundNamespaces, Is.Not.Null, "Previous tests must be run first");
        
        var service = new TypeToolService(_scanner);
        var response = service.GetTypes(_testProjectPath, _foundNamespaces, new List<string>(), 1, 50);

        Assert.That(response.TypeData, Is.Not.Null);
        Assert.That(response.TypeData.Count, Is.GreaterThan(0));
    }
}

