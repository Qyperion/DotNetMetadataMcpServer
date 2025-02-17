using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Services;

namespace MetadataExplorerTest
{
    // Fake implementation for testing purposes.
    public class FakeDependenciesScanner : IDependenciesScanner
    {
        public ProjectMetadata ScanProject(string projectFileAbsolutePath)
        {
            return new ProjectMetadata 
            {
                AssemblyPath = "/dummy/path/MyMainAssembly.dll",
                ProjectTypes = new List<TypeInfoModel>
                {
                    new TypeInfoModel { FullName = "MyNamespace.MyType" },
                    new TypeInfoModel { FullName = "OtherNamespace.MyOtherType" }
                },
                Dependencies = new List<DependencyInfo>
                {
                    new DependencyInfo 
                    {
                        Name = "DepAssembly.dll",
                        Types = new List<TypeInfoModel>
                        {
                            new TypeInfoModel { FullName = "DepNamespace.DepType" }
                        }
                    }
                }
            };
        }

        public void Dispose() { }
    }

    [TestFixture]
    public class FilteringTests
    {
        private IDependenciesScanner _fakeScanner;
        private const string DummyProjectPath = "/dummy/path/project.csproj";

        [SetUp]
        public void Setup()
        {
            _fakeScanner = new FakeDependenciesScanner();
        }
        
        [TearDown]
        public void Dispose()
        {
            _fakeScanner.Dispose();
        }
        

        [Test]
        public void TypeToolService_FiltersByAllowedNamespaces_AndPaginates()
        {
            // Arrange
            var service = new TypeToolService(_fakeScanner);
            var allowedNamespaces = new List<string> { "MyNamespace" };
            var filters = new List<string>(); // no additional filter
            int pageNumber = 1;
            int pageSize = 10;

            // Act
            var response = service.GetTypes(DummyProjectPath, allowedNamespaces, filters, pageNumber, pageSize);

            // Assert: Only "MyNamespace.MyType" should be returned.
            var types = response.TypeData.ToList();
            Assert.That(types.Count, Is.EqualTo(1));
            Assert.That(types.First().FullName, Is.EqualTo("MyNamespace.MyType"));
            Assert.That(response.CurrentPage, Is.EqualTo(pageNumber));
        }

        [Test]
        public void NamespaceToolService_FiltersByAllowedAssemblyNames_AndPaginates()
        {
            // Arrange
            var service = new NamespaceToolService(_fakeScanner);
            // Main assembly is allowed and dependency is allowed.
            var allowedAssemblyNames = new List<string> { "MyMainAssembly.dll", "DepAssembly.dll" };
            var filters = new List<string>(); // no additional filter
            int pageNumber = 1;
            int pageSize = 10;

            // Act
            var response = service.GetNamespaces(DummyProjectPath, allowedAssemblyNames, filters, pageNumber, pageSize);

            // Assert: Expect namespaces "MyNamespace", "OtherNamespace", and "DepNamespace" (distinct).
            var nsList = response.Namespaces.ToList();
            Assert.That(nsList.Count, Is.EqualTo(3));
            Assert.That(nsList, Does.Contain("MyNamespace"));
            Assert.That(nsList, Does.Contain("OtherNamespace"));
            Assert.That(nsList, Does.Contain("DepNamespace"));
            Assert.That(response.CurrentPage, Is.EqualTo(pageNumber));
        }

        [Test]
        public void AssemblyToolService_AppliesFilter_AndPaginates()
        {
            // Arrange
            var service = new AssemblyToolService(_fakeScanner);
            // Use filter to match dependency assembly only.
            var filters = new List<string> { "Dep*" };
            int pageNumber = 1;
            int pageSize = 10;

            // Act
            var response = service.GetAssemblies(DummyProjectPath, filters, pageNumber, pageSize);

            // Assert: Only "DepAssembly.dll" should match the filter.
            var assemblies = response.AssemblyNames.ToList();
            Assert.That(assemblies.Count, Is.EqualTo(1));
            Assert.That(assemblies.First(), Is.EqualTo("DepAssembly.dll"));
            Assert.That(response.CurrentPage, Is.EqualTo(pageNumber));
        }
    }
}
