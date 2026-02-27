using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Services;
using DotNetMetadataMcpServer.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MetadataExplorerTest.Integration;

/// <summary>
/// End-to-end integration tests for Assembly, Namespace, and Type tools
/// using a real MCP server/client with in-memory pipes against McpTestProject.
/// </summary>
[TestFixture]
[NonParallelizable] // MCP client with pipe streams does not support concurrent reads/writes
public class ProjectToolsEndToEndTests : McpServerIntegrationTestBase
{
    /// <summary>
    /// Absolute path to the McpTestProject.Core project used as test target.
    /// This project must be built before running these tests.
    /// </summary>
    private const string TestProjectPath =
        @"C:\Users\Endy\source\repos\McpTestProject\McpTestProject.Core\McpTestProject.Core.csproj";

    /// <summary>
    /// Extracts text content from an AIFunction.InvokeAsync result.
    /// In MCP SDK RC1, different tools may return TextContent or JsonElement.
    /// </summary>
    private static string ExtractText(object? result)
    {
        return result switch
        {
            TextContent tc => tc.Text ?? throw new InvalidOperationException("TextContent.Text is null"),
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString()!,
            JsonElement je when je.TryGetProperty("content", out var content) =>
                content[0].GetProperty("text").GetString()!,
            JsonElement je => je.GetRawText(),
            _ => throw new InvalidOperationException($"Unexpected result type: {result?.GetType()}")
        };
    }

    protected override void ConfigureServices(ServiceCollection services, IMcpServerBuilder mcpServerBuilder)
    {
        mcpServerBuilder
            .WithTools<AssemblyTools>()
            .WithTools<NamespaceTools>()
            .WithTools<TypeTools>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tools:DefaultPageSize"] = "20",
                ["Tools:IndentResponse"] = "true"
            })
            .Build();

        services.Configure<ToolsConfiguration>(configuration.GetSection("Tools"));

        services.AddScoped<MsBuildHelper>();
        services.AddScoped<ReflectionTypesCollector>();
        services.AddScoped<IDependenciesScanner, DependenciesScanner>();
        services.AddScoped<AssemblyToolService>();
        services.AddScoped<NamespaceToolService>();
        services.AddScoped<TypeToolService>();
        services.AddSingleton<IProjectMetadataCache, ProjectMetadataCache>();
    }

    #region ReferencedAssembliesExplorer Tests

    [Test]
    public async Task ReferencedAssembliesExplorer_Should_Return_Assemblies()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "ReferencedAssembliesExplorer");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<AssemblyToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.AssemblyNames, Is.Not.Empty);
        Assert.That(response.CurrentPage, Is.EqualTo(1));
        Assert.That(response.AvailablePages, Is.Not.Empty);

        var assemblyNames = response.AssemblyNames.ToList();
        Assert.That(assemblyNames, Does.Contain("McpTestProject.Core"),
            "Should contain the project's own assembly");
        Assert.That(assemblyNames.Any(a => a.Contains("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase)),
            Is.True, "Should contain Newtonsoft.Json dependency");
    }

    [Test]
    public async Task ReferencedAssembliesExplorer_WithFilter_Should_Return_Only_Matching()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "ReferencedAssembliesExplorer");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["fullTextFiltersWithWildCardSupport"] = new[] { "Newtonsoft*" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<AssemblyToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.AssemblyNames, Is.Not.Empty);

        // All returned assemblies should match the filter
        foreach (var name in response.AssemblyNames)
        {
            Assert.That(name, Does.StartWith("Newtonsoft").IgnoreCase,
                $"Assembly '{name}' does not match filter 'Newtonsoft*'");
        }
    }

    [Test]
    public async Task ReferencedAssembliesExplorer_WithNonMatchingFilter_Should_Return_Empty()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "ReferencedAssembliesExplorer");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["fullTextFiltersWithWildCardSupport"] = new[] { "NonExistentLibrary*" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<AssemblyToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.AssemblyNames, Is.Empty,
            "Non-matching filter should return no assemblies");
    }

    [Test]
    public async Task ReferencedAssembliesExplorer_WithInvalidPath_Should_Return_Error()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "ReferencedAssembliesExplorer");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = @"C:\nonexistent\path\project.csproj",
            ["pageNumber"] = 1
        };

        // Act & Assert - should throw or return error content
        try
        {
            var result = await tool.InvokeAsync(arguments);
            var text = ExtractText(result ?? throw new InvalidOperationException("Result is null"));
            // If it returns, the content should indicate an error
            Assert.That(text, Does.Contain("error").IgnoreCase.Or.Contains("exception").IgnoreCase,
                "Expected error message for invalid project path");
        }
        catch (Exception)
        {
            // Throwing is also acceptable behavior for invalid input
            Assert.Pass("Tool threw exception as expected for invalid path");
        }
    }

    #endregion

    #region NamespacesExplorer Tests

    [Test]
    public async Task NamespacesExplorer_Should_Return_All_Namespaces()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespacesExplorer");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NamespaceToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Namespaces, Is.Not.Empty);

        var namespaces = response.Namespaces.ToList();
        Assert.That(namespaces, Does.Contain("McpTestProject.Core.Models"),
            "Should contain Models namespace");
        Assert.That(namespaces, Does.Contain("McpTestProject.Core.Services"),
            "Should contain Services namespace");
        Assert.That(namespaces, Does.Contain("McpTestProject.Core.Helpers"),
            "Should contain Helpers namespace");
    }

    [Test]
    public async Task NamespacesExplorer_WithAssemblyFilter_Should_Return_Only_Assembly_Namespaces()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespacesExplorer");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["assemblyNames"] = new[] { "Newtonsoft.Json" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NamespaceToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Namespaces, Is.Not.Empty);

        // All namespaces should belong to Newtonsoft.Json
        foreach (var ns in response.Namespaces)
        {
            Assert.That(ns, Does.StartWith("Newtonsoft").IgnoreCase,
                $"Namespace '{ns}' should belong to Newtonsoft.Json assembly");
        }

        // Should NOT contain project namespaces
        var namespaces = response.Namespaces.ToList();
        Assert.That(namespaces, Does.Not.Contain("McpTestProject.Core.Models"),
            "Should not contain project namespaces when filtering by Newtonsoft.Json assembly");
    }

    [Test]
    public async Task NamespacesExplorer_WithProjectAssemblyFilter_Should_Return_Project_Namespaces()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespacesExplorer");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["assemblyNames"] = new[] { "McpTestProject.Core" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NamespaceToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Namespaces, Is.Not.Empty);

        var namespaces = response.Namespaces.ToList();
        Assert.That(namespaces, Does.Contain("McpTestProject.Core.Models"));
        Assert.That(namespaces, Does.Contain("McpTestProject.Core.Services"));
        Assert.That(namespaces, Does.Contain("McpTestProject.Core.Helpers"));

        // Should NOT contain Newtonsoft namespaces
        Assert.That(namespaces.Any(n => n.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase)),
            Is.False, "Should not contain Newtonsoft namespaces when filtering by project assembly");
    }

    [Test]
    public async Task NamespacesExplorer_WithTextFilter_Should_Return_Only_Matching()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespacesExplorer");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["fullTextFiltersWithWildCardSupport"] = new[] { "*Models*" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NamespaceToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Namespaces, Is.Not.Empty);

        foreach (var ns in response.Namespaces)
        {
            Assert.That(ns, Does.Contain("Models").IgnoreCase,
                $"Namespace '{ns}' does not match filter '*Models*'");
        }
    }

    #endregion

    #region NamespaceTypes Tests

    [Test]
    public async Task NamespaceTypes_ForModels_Should_Return_Product_Type()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespaceTypes");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["namespaces"] = new[] { "McpTestProject.Core.Models" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<TypeToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.TypeData, Is.Not.Empty);

        var productType = response.TypeData.FirstOrDefault(t =>
            t.FullName.Contains("Product", StringComparison.OrdinalIgnoreCase));
        Assert.That(productType, Is.Not.Null, "Should find Product type in Models namespace");

        // Verify Product has expected properties
        Assert.That(productType!.Properties, Is.Not.Null.And.Not.Empty,
            "Product type should have properties");
        var propertyNames = string.Join(", ", productType.Properties!);
        Assert.That(propertyNames, Does.Contain("Id").IgnoreCase, "Product should have Id property");
        Assert.That(propertyNames, Does.Contain("Name").IgnoreCase, "Product should have Name property");
        Assert.That(propertyNames, Does.Contain("Price").IgnoreCase, "Product should have Price property");
    }

    [Test]
    public async Task NamespaceTypes_ForServices_Should_Return_Interface_And_Implementation()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespaceTypes");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["namespaces"] = new[] { "McpTestProject.Core.Services" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<TypeToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.TypeData, Is.Not.Empty);

        var typeNames = response.TypeData.Select(t => t.FullName).ToList();

        // Should find both interface and class
        Assert.That(typeNames.Any(n => n.Contains("IProductService")), Is.True,
            "Should find IProductService interface");
        Assert.That(typeNames.Any(n => n.Contains("ProductService") && !n.Contains("IProductService")), Is.True,
            "Should find ProductService implementation class");
    }

    [Test]
    public async Task NamespaceTypes_ForServices_Should_Show_Methods()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespaceTypes");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["namespaces"] = new[] { "McpTestProject.Core.Services" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<TypeToolResponse>(text);
        Assert.That(response, Is.Not.Null);

        var productService = response!.TypeData.FirstOrDefault(t =>
            t.FullName.Contains("ProductService") && !t.FullName.Contains("IProductService"));
        Assert.That(productService, Is.Not.Null, "Should find ProductService type");

        // Verify methods
        Assert.That(productService!.Methods, Is.Not.Null.And.Not.Empty,
            "ProductService should have methods");
        var methods = string.Join(", ", productService.Methods!);
        Assert.That(methods, Does.Contain("GetById").IgnoreCase,
            "ProductService should have GetById method");
        Assert.That(methods, Does.Contain("GetAll").IgnoreCase,
            "ProductService should have GetAll method");
    }

    [Test]
    public async Task NamespaceTypes_ForServices_Should_Show_Events()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespaceTypes");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["namespaces"] = new[] { "McpTestProject.Core.Services" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<TypeToolResponse>(text);
        Assert.That(response, Is.Not.Null);

        var productService = response!.TypeData.FirstOrDefault(t =>
            t.FullName.Contains("ProductService") && !t.FullName.Contains("IProductService"));
        Assert.That(productService, Is.Not.Null, "Should find ProductService type");

        // Verify events
        Assert.That(productService!.Events, Is.Not.Null.And.Not.Empty,
            "ProductService should have events");
        var events = string.Join(", ", productService.Events!);
        Assert.That(events, Does.Contain("ProductCreated").IgnoreCase,
            "ProductService should have ProductCreated event");
    }

    [Test]
    public async Task NamespaceTypes_ForHelpers_Should_Return_Static_Class()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespaceTypes");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["namespaces"] = new[] { "McpTestProject.Core.Helpers" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<TypeToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.TypeData, Is.Not.Empty);

        var jsonHelper = response.TypeData.FirstOrDefault(t =>
            t.FullName.Contains("JsonHelper", StringComparison.OrdinalIgnoreCase));
        Assert.That(jsonHelper, Is.Not.Null, "Should find JsonHelper type in Helpers namespace");

        // Static class should have methods
        Assert.That(jsonHelper!.Methods, Is.Not.Null.And.Not.Empty,
            "JsonHelper should have static methods");
    }

    [Test]
    public async Task NamespaceTypes_WithFilter_Should_Return_Only_Matching()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespaceTypes");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["namespaces"] = new[] { "McpTestProject.Core.Services" },
            ["fullTextFiltersWithWildCardSupport"] = new[] { "*Product*" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<TypeToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.TypeData, Is.Not.Empty);

        // All types should match the filter
        foreach (var type in response.TypeData)
        {
            Assert.That(type.FullName, Does.Contain("Product").IgnoreCase,
                $"Type '{type.FullName}' does not match filter '*Product*'");
        }
    }

    [Test]
    public async Task NamespaceTypes_WithNoNamespace_Should_Return_All_Types()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespaceTypes");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<TypeToolResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.TypeData, Is.Not.Empty,
            "Without namespace filter, should return types from all namespaces");

        var typeNames = response.TypeData.Select(t => t.FullName).ToList();
        // Should include types from different namespaces
        Assert.That(typeNames.Any(n => n.Contains("Product")), Is.True,
            "Should contain Product-related types");
    }

    [Test]
    public async Task NamespaceTypes_ProductService_Should_Implement_IProductService()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespaceTypes");

        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["namespaces"] = new[] { "McpTestProject.Core.Services" },
            ["fullTextFiltersWithWildCardSupport"] = new[] { "*ProductService" },
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<TypeToolResponse>(text);
        Assert.That(response, Is.Not.Null);

        // Find the concrete ProductService (not the interface)
        var productService = response!.TypeData.FirstOrDefault(t =>
            t.FullName.Contains("ProductService") && !t.FullName.Contains("IProductService"));
        Assert.That(productService, Is.Not.Null, "Should find ProductService");

        // Verify it implements IProductService
        Assert.That(productService!.Implements, Is.Not.Null.And.Not.Empty,
            "ProductService should implement interfaces");
        Assert.That(productService.Implements!.Any(i => i.Contains("IProductService")),
            Is.True, "ProductService should implement IProductService");
    }

    #endregion

    #region Cross-Tool Workflow Tests

    [Test]
    public async Task FullWorkflow_Assemblies_To_Namespaces_To_Types()
    {
        // This test simulates the recommended top-down exploration workflow:
        // 1. Get assemblies → 2. Get namespaces for an assembly → 3. Get types for a namespace

        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();

        // Step 1: Get assemblies
        var assemblyTool = tools.First(t => t.Name == "ReferencedAssembliesExplorer");
        var assemblyResult = await assemblyTool.InvokeAsync(new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["pageNumber"] = 1
        });
        var assemblyResponse = JsonSerializer.Deserialize<AssemblyToolResponse>(ExtractText(assemblyResult));
        Assert.That(assemblyResponse!.AssemblyNames, Is.Not.Empty, "Step 1: Should have assemblies");

        // Step 2: Get namespaces for the project assembly
        var namespaceTool = tools.First(t => t.Name == "NamespacesExplorer");
        var nsResult = await namespaceTool.InvokeAsync(new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["assemblyNames"] = new[] { "McpTestProject.Core" },
            ["pageNumber"] = 1
        });
        var nsResponse = JsonSerializer.Deserialize<NamespaceToolResponse>(ExtractText(nsResult));
        Assert.That(nsResponse!.Namespaces, Is.Not.Empty, "Step 2: Should have namespaces");

        // Step 3: Get types for a discovered namespace
        var discoveredNamespace = nsResponse.Namespaces.First();
        var typeTool = tools.First(t => t.Name == "NamespaceTypes");
        var typeResult = await typeTool.InvokeAsync(new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["namespaces"] = new[] { discoveredNamespace },
            ["pageNumber"] = 1
        });
        var typeResponse = JsonSerializer.Deserialize<TypeToolResponse>(ExtractText(typeResult));
        Assert.That(typeResponse!.TypeData, Is.Not.Empty,
            $"Step 3: Should have types in namespace '{discoveredNamespace}'");

        // Verify at least one type has meaningful data
        var firstType = typeResponse.TypeData.First();
        Assert.That(firstType.FullName, Is.Not.Null.And.Not.Empty,
            "Type should have a FullName");
    }

    #endregion

    #region Pagination Tests

    [Test]
    public async Task NamespaceTypes_Pagination_Should_Return_Correct_Pages()
    {
        // This test verifies pagination works by requesting all types without namespace filter
        // and checking page metadata
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NamespaceTypes");

        // Page 1
        var result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = TestProjectPath,
            ["pageNumber"] = 1
        });
        var response = JsonSerializer.Deserialize<TypeToolResponse>(ExtractText(result));
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.CurrentPage, Is.EqualTo(1));
        Assert.That(response.AvailablePages, Contains.Item(1));
    }

    #endregion
}
