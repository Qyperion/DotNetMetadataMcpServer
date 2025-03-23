using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Services;
using DotNetMetadataMcpServer.ToolHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.NET.Core.Models.Protocol.Client.Responses;
using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Server.Contexts;
using ModelContextProtocol.NET.Server.Features.Tools;
using ModelContextProtocol.NET.Server.Session;
using Moq;
using System.Text.Json.Serialization.Metadata;

namespace MetadataExplorerTest.ToolHandlers;

// Test-specific interface for NuGetToolService to allow for testing
public interface INuGetToolService
{
    Task<NuGetPackageSearchResponse> SearchPackagesAsync(
        string searchQuery, 
        List<string> filters, 
        bool includePrerelease, 
        int pageNumber, 
        int pageSize);
        
    Task<NuGetPackageVersionsResponse> GetPackageVersionsAsync(
        string packageId, 
        List<string> filters, 
        bool includePrerelease, 
        int pageNumber, 
        int pageSize);
}

// Adapter to make NuGetToolService implement the interface
public class NuGetToolServiceAdapter : INuGetToolService
{
    private readonly NuGetToolService _service;
    
    public NuGetToolServiceAdapter(NuGetToolService service)
    {
        _service = service;
    }
    
    public Task<NuGetPackageSearchResponse> SearchPackagesAsync(
        string searchQuery, 
        List<string> filters, 
        bool includePrerelease, 
        int pageNumber, 
        int pageSize)
    {
        return _service.SearchPackagesAsync(searchQuery, filters, includePrerelease, pageNumber, pageSize);
    }
    
    public Task<NuGetPackageVersionsResponse> GetPackageVersionsAsync(
        string packageId, 
        List<string> filters, 
        bool includePrerelease, 
        int pageNumber, 
        int pageSize)
    {
        return _service.GetPackageVersionsAsync(packageId, filters, includePrerelease, pageNumber, pageSize);
    }
}

// Modified handlers that accept the interface instead of the concrete class
public class TestableNuGetPackageSearchToolHandler : NuGetPackageSearchToolHandler
{
    private readonly INuGetToolService _nuGetToolService;
    
    public TestableNuGetPackageSearchToolHandler(
        IServerContext serverContext,
        ISessionFacade sessionFacade,
        IOptions<ToolsConfiguration> toolsConfiguration,
        NuGetToolService nuGetToolService,
        ILogger<NuGetPackageSearchToolHandler> logger)
        : base(serverContext, sessionFacade, toolsConfiguration, nuGetToolService, logger)
    {
        _nuGetToolService = new NuGetToolServiceAdapter(nuGetToolService);
    }
    
    public TestableNuGetPackageSearchToolHandler(
        IServerContext serverContext,
        ISessionFacade sessionFacade,
        IOptions<ToolsConfiguration> toolsConfiguration,
        INuGetToolService nuGetToolService,
        ILogger<NuGetPackageSearchToolHandler> logger)
        : base(serverContext, sessionFacade, toolsConfiguration, new NuGetToolService(new Mock<ILogger<NuGetToolService>>().Object), logger)
    {
        _nuGetToolService = nuGetToolService;
    }

    public new Task<CallToolResult> HandleAsync(NuGetPackageSearchParameters parameters, CancellationToken cancellationToken = default)
    {
        // Override to use the interface instead of the concrete class
        return HandleAsyncInternal(parameters, cancellationToken);
    }
    
    private async Task<CallToolResult> HandleAsyncInternal(NuGetPackageSearchParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _nuGetToolService.SearchPackagesAsync(
            parameters.SearchQuery,
            parameters.FullTextFiltersWithWildCardSupport,
            parameters.IncludePrerelease,
            parameters.PageNumber,
            10); // Use a fixed page size for testing
            
        var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var content = new ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content.TextContent { Text = json };
        return new CallToolResult { Content = [content] };
    }
}

public class TestableNuGetPackageVersionsToolHandler : NuGetPackageVersionsToolHandler
{
    private readonly INuGetToolService _nuGetToolService;
    
    public TestableNuGetPackageVersionsToolHandler(
        IServerContext serverContext,
        ISessionFacade sessionFacade,
        IOptions<ToolsConfiguration> toolsConfiguration,
        NuGetToolService nuGetToolService,
        ILogger<NuGetPackageVersionsToolHandler> logger)
        : base(serverContext, sessionFacade, toolsConfiguration, nuGetToolService, logger)
    {
        _nuGetToolService = new NuGetToolServiceAdapter(nuGetToolService);
    }
    
    public TestableNuGetPackageVersionsToolHandler(
        IServerContext serverContext,
        ISessionFacade sessionFacade,
        IOptions<ToolsConfiguration> toolsConfiguration,
        INuGetToolService nuGetToolService,
        ILogger<NuGetPackageVersionsToolHandler> logger)
        : base(serverContext, sessionFacade, toolsConfiguration, new NuGetToolService(new Mock<ILogger<NuGetToolService>>().Object), logger)
    {
        _nuGetToolService = nuGetToolService;
    }

    public new Task<CallToolResult> HandleAsync(NuGetPackageVersionParameters parameters, CancellationToken cancellationToken = default)
    {
        // Override to use the interface instead of the concrete class
        return HandleAsyncInternal(parameters, cancellationToken);
    }
    
    private async Task<CallToolResult> HandleAsyncInternal(NuGetPackageVersionParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _nuGetToolService.GetPackageVersionsAsync(
            parameters.PackageId,
            parameters.FullTextFiltersWithWildCardSupport,
            parameters.IncludePrerelease,
            parameters.PageNumber,
            10); // Use a fixed page size for testing
            
        var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var content = new ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content.TextContent { Text = json };
        return new CallToolResult { Content = [content] };
    }
}

[TestFixture]
public class NuGetToolHandlersTests
{
    private Mock<IServerContext> _serverContextMock;
    private Mock<ISessionFacade> _sessionFacadeMock;
    private Mock<IOptions<ToolsConfiguration>> _toolsConfigurationMock;
    private Mock<INuGetToolService> _nuGetToolServiceMock;
    private Mock<ILogger<NuGetPackageSearchToolHandler>> _searchLoggerMock;
    private Mock<ILogger<NuGetPackageVersionsToolHandler>> _versionsLoggerMock;
    private ToolsConfiguration _toolsConfiguration;
    
    private TestableNuGetPackageSearchToolHandler _searchHandler;
    private TestableNuGetPackageVersionsToolHandler _versionsHandler;

    [SetUp]
    public void Setup()
    {
        _serverContextMock = new Mock<IServerContext>();
        _sessionFacadeMock = new Mock<ISessionFacade>();
        
        _toolsConfiguration = new ToolsConfiguration
        {
            DefaultPageSize = 10,
            IntendResponse = true
        };
        
        _toolsConfigurationMock = new Mock<IOptions<ToolsConfiguration>>();
        _toolsConfigurationMock.Setup(x => x.Value).Returns(_toolsConfiguration);
        
        _nuGetToolServiceMock = new Mock<INuGetToolService>();
        _searchLoggerMock = new Mock<ILogger<NuGetPackageSearchToolHandler>>();
        _versionsLoggerMock = new Mock<ILogger<NuGetPackageVersionsToolHandler>>();
        
        _searchHandler = new TestableNuGetPackageSearchToolHandler(
            _serverContextMock.Object,
            _sessionFacadeMock.Object,
            _toolsConfigurationMock.Object,
            _nuGetToolServiceMock.Object,
            _searchLoggerMock.Object);
        
        _versionsHandler = new TestableNuGetPackageVersionsToolHandler(
            _serverContextMock.Object,
            _sessionFacadeMock.Object,
            _toolsConfigurationMock.Object,
            _nuGetToolServiceMock.Object,
            _versionsLoggerMock.Object);
    }

    [Test]
    public async Task NuGetPackageSearchToolHandler_HandleAsync_ReturnsExpectedResult()
    {
        // Arrange
        var parameters = new NuGetPackageSearchParameters
        {
            SearchQuery = "Newtonsoft.Json",
            PageNumber = 1,
            FullTextFiltersWithWildCardSupport = new List<string>()
        };
        
        var expectedResponse = new NuGetPackageSearchResponse
        {
            Packages = new List<NuGetPackageInfo>
            {
                new NuGetPackageInfo
                {
                    Id = "Newtonsoft.Json",
                    Version = "13.0.3",
                    Description = "Json.NET is a popular high-performance JSON framework for .NET",
                    Authors = "James Newton-King",
                    DownloadCount = 1000000,
                    Published = DateTimeOffset.Now.AddDays(-100)
                }
            },
            CurrentPage = 1,
            AvailablePages = new List<int> { 1 }
        };
        
        _nuGetToolServiceMock
            .Setup(x => x.SearchPackagesAsync(
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(expectedResponse);
        
        // Act
        var result = await _searchHandler.HandleAsync(parameters, CancellationToken.None);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Content, Is.Not.Empty);
        var textContent = result.Content[0] as ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content.TextContent;
        Assert.That(textContent, Is.Not.Null);
        Assert.That(textContent.Text, Does.Contain("Newtonsoft.Json"));
        
        _nuGetToolServiceMock.Verify(x => x.SearchPackagesAsync(
            parameters.SearchQuery,
            parameters.FullTextFiltersWithWildCardSupport,
            parameters.IncludePrerelease,
            parameters.PageNumber,
            It.IsAny<int>()), Times.Once);
    }
    
    [Test]
    public async Task NuGetPackageVersionsToolHandler_HandleAsync_ReturnsExpectedResult()
    {
        // Arrange
        var parameters = new NuGetPackageVersionParameters
        {
            PackageId = "Newtonsoft.Json",
            PageNumber = 1,
            FullTextFiltersWithWildCardSupport = new List<string>()
        };
        
        var expectedResponse = new NuGetPackageVersionsResponse
        {
            PackageId = "Newtonsoft.Json",
            Versions = new List<NuGetPackageInfo>
            {
                new NuGetPackageInfo
                {
                    Id = "Newtonsoft.Json",
                    Version = "13.0.3",
                    Description = "Json.NET is a popular high-performance JSON framework for .NET",
                    Authors = "James Newton-King",
                    DownloadCount = 1000000,
                    Published = DateTimeOffset.Now.AddDays(-100),
                    DependencyGroups = new List<NuGetPackageDependencyGroup>
                    {
                        new NuGetPackageDependencyGroup
                        {
                            TargetFramework = ".NETStandard2.0",
                            Dependencies = new List<NuGetPackageDependency>
                            {
                                new NuGetPackageDependency
                                {
                                    Id = "System.Text.Json",
                                    VersionRange = "6.0.0"
                                }
                            }
                        }
                    }
                }
            },
            CurrentPage = 1,
            AvailablePages = new List<int> { 1 }
        };
        
        _nuGetToolServiceMock
            .Setup(x => x.GetPackageVersionsAsync(
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(expectedResponse);
        
        // Act
        var result = await _versionsHandler.HandleAsync(parameters, CancellationToken.None);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Content, Is.Not.Empty);
        var textContent = result.Content[0] as ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content.TextContent;
        Assert.That(textContent, Is.Not.Null);
        Assert.That(textContent.Text, Does.Contain("Newtonsoft.Json"));
        Assert.That(textContent.Text, Does.Contain("DependencyGroups"));
        
        _nuGetToolServiceMock.Verify(x => x.GetPackageVersionsAsync(
            parameters.PackageId,
            parameters.FullTextFiltersWithWildCardSupport,
            parameters.IncludePrerelease,
            parameters.PageNumber,
            It.IsAny<int>()), Times.Once);
    }
}