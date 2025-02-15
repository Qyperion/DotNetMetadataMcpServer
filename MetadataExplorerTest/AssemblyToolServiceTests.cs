using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Services;
using Moq;

namespace MetadataExplorerTest;

[TestFixture]
public class AssemblyToolServiceTests
{
    private string _testProjectPath;
    private DependenciesScanner _scanner;

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
    
    
    [Test]
    public void TypeToolService_RealScan_Returns_ValidResponse()
    {
        // Arrange
        var service = new TypeToolService(_scanner);
        // Use an empty allowedNamespaces so that all types are returned.
        var allowedNamespaces = new List<string>();
        var filters = new List<string>(); 
        int pageNumber = 1;
        int pageSize = 20;

        // Act
        var response = service.GetTypes(_testProjectPath, allowedNamespaces, filters, pageNumber, pageSize);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.TypeData, Is.Not.Null);
        Assert.That(response.CurrentPage, Is.EqualTo(pageNumber));
        //Assert.That(response.AvailablePages, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void NamespaceToolService_RealScan_Returns_ValidResponse()
    {
        // Arrange
        var service = new NamespaceToolService(_scanner);
        // If unsure about assembly names, use an empty allowed list
        var allowedAssemblyNames = new List<string>(); 
        var filters = new List<string>(); 
        int pageNumber = 1;
        int pageSize = 20;

        // Act
        var response = service.GetNamespaces(_testProjectPath, allowedAssemblyNames, filters, pageNumber, pageSize);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Namespaces, Is.Not.Null);
        Assert.That(response.CurrentPage, Is.EqualTo(pageNumber));
        //Assert.That(response.AvailablePages, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void AssemblyToolService_RealScan_Returns_ValidResponse()
    {
        // Arrange
        var service = new AssemblyToolService(_scanner);
        var filters = new List<string>(); 
        int pageNumber = 1;
        int pageSize = 20;

        // Act
        var response = service.GetAssemblies(_testProjectPath, filters, pageNumber, pageSize);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.AssemblyNames, Is.Not.Null);
        Assert.That(response.CurrentPage, Is.EqualTo(pageNumber));
        //Assert.That(response.AvailablePages, Is.GreaterThanOrEqualTo(1));
    }
}

