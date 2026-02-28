using DotNetMetadataMcpServer;
using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Models.Base;
using DotNetMetadataMcpServer.Services;
using DotNetMetadataMcpServer.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MetadataExplorerTest.Integration;

/// <summary>
/// End-to-end integration tests that verify tools work correctly through the full MCP protocol stack.
/// These tests use a real MCP server and client connected through in-memory pipes.
/// </summary>
[TestFixture]
[NonParallelizable] // MCP client with pipe streams does not support concurrent reads/writes
public class EndToEndToolTests : McpServerIntegrationTestBase
{
    /// <summary>
    /// Extracts text content from an AIFunction.InvokeAsync result.
    /// In MCP SDK RC1, different tools may return TextContent or JsonElement.
    /// </summary>
    private static string ExtractText(object? result)
    {
        return result switch
        {
            TextContent tc => tc.Text ?? throw new InvalidOperationException("TextContent.Text is null"),
            JsonElement { ValueKind: JsonValueKind.String } je => je.GetString()!,
            JsonElement je when je.TryGetProperty("content", out var content) =>
                content[0].GetProperty("text").GetString()!,
            JsonElement je => je.GetRawText(),
            _ => throw new InvalidOperationException($"Unexpected result type: {result?.GetType()}")
        };
    }
    protected override void ConfigureServices(ServiceCollection services, IMcpServerBuilder mcpServerBuilder)
    {
        // Configure all MCP tools
        mcpServerBuilder
            .WithTools<AssemblyTools>()
            .WithTools<NamespaceTools>()
            .WithTools<TypeTools>()
            .WithTools<TypeSearchTools>()
            .WithTools<InheritanceTools>()
            .WithTools<DependencyGraphTools>()
            .WithTools<NuGetTools>();

        // Configure settings
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tools:DefaultPageSize"] = "20",
                ["Tools:IndentResponse"] = "true" // Use indented JSON for easier debugging
            })
            .Build();

        services.Configure<ToolsConfiguration>(configuration.GetSection("Tools"));

        // Register all services as scoped
        services.AddScoped<MsBuildHelper>();
        services.AddScoped<ReflectionTypesCollector>();
        services.AddScoped<IDependenciesScanner, DependenciesScanner>();
        services.AddScoped<AssemblyToolService>();
        services.AddScoped<NamespaceToolService>();
        services.AddScoped<TypeToolService>();
        services.AddScoped<TypeSearchToolService>();
        services.AddScoped<InheritanceToolService>();
        services.AddScoped<DependencyGraphToolService>();
        services.AddScoped<NuGetToolService>();
        services.AddSingleton<IProjectMetadataCache, ProjectMetadataCache>();
    }

    [Test]
    public async Task ListTools_Should_Return_All_Registered_Tools()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();

        // Act
        var tools = await client.ListToolsAsync();

        // Assert
        Assert.That(tools, Is.Not.Empty);

        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.That(toolNames, Does.Contain("ReferencedAssembliesExplorer"));
        Assert.That(toolNames, Does.Contain("NamespacesExplorer"));
        Assert.That(toolNames, Does.Contain("NamespaceTypes"));
        Assert.That(toolNames, Does.Contain("TypeSearch"));
        Assert.That(toolNames, Does.Contain("InheritanceHierarchy"));
        Assert.That(toolNames, Does.Contain("DependencyGraphExplorer"));
        Assert.That(toolNames, Does.Contain("NuGetPackageSearch"));
        Assert.That(toolNames, Does.Contain("NuGetPackageVersions"));
    }

    [Test]
    public async Task NuGetPackageSearch_Should_Return_Results()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var searchTool = tools.First(t => t.Name == "NuGetPackageSearch");

        // Act
        var arguments = new AIFunctionArguments
        {
            ["searchQuery"] = "Newtonsoft.Json",
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };

        var result = await searchTool.InvokeAsync(arguments);

        // Assert
        Assert.That(result, Is.Not.Null);

        var textContent = ExtractText(result);
        Assert.That(textContent, Is.Not.Null.And.Not.Empty);

        var response = JsonSerializer.Deserialize<NuGetPackageSearchResponse>(textContent);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Packages, Is.Not.Empty);

        var hasNewtonsoftJson = response.Packages.Any(p =>
            p.Id.Contains("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase));
        Assert.That(hasNewtonsoftJson, Is.True, "Expected to find Newtonsoft.Json in search results");
    }

    [Test]
    public async Task NuGetPackageVersions_Should_Return_Version_History()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var versionsTool = tools.First(t => t.Name == "NuGetPackageVersions");

        // Act
        var arguments = new AIFunctionArguments
        {
            ["packageId"] = "Newtonsoft.Json",
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };

        var result = await versionsTool.InvokeAsync(arguments);

        // Assert
        Assert.That(result, Is.Not.Null);

        var textContent = ExtractText(result);
        Assert.That(textContent, Is.Not.Null.And.Not.Empty);

        var response = JsonSerializer.Deserialize<NuGetPackageVersionsResponse>(textContent);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.PackageId, Is.EqualTo("Newtonsoft.Json"));
        Assert.That(response.Versions, Is.Not.Empty);
    }

    [Test]
    public async Task Tools_Should_Have_Proper_Descriptions_And_Parameters()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();

        // Act
        var tools = await client.ListToolsAsync();

        // Assert
        foreach (var tool in tools)
        {
            Assert.That(tool.Name, Is.Not.Null.And.Not.Empty,
                "Tool should have a name");
            Assert.That(tool.Description, Is.Not.Null.And.Not.Empty,
                $"Tool {tool.Name} should have a description");

            // Verify the tool has JSON schema
            Assert.That(tool.JsonSchema.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined),
                $"Tool {tool.Name} should have a JSON schema defined");
        }
    }

    [Test]
    public async Task Multiple_Tool_Invocations_Should_Work_Independently()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var searchTool = tools.First(t => t.Name == "NuGetPackageSearch");

        // Act - Invoke the same tool multiple times
        var results = new List<object?>();
        for (int i = 0; i < 3; i++)
        {
            var arguments = new AIFunctionArguments
            {
                ["searchQuery"] = "System.Text.Json",
                ["includePrerelease"] = false,
                ["pageNumber"] = 1
            };

            var result = await searchTool.InvokeAsync(arguments);
            results.Add(result);
        }

        // Assert - All invocations should succeed
        Assert.That(results, Has.Count.EqualTo(3));

        foreach (var result in results)
        {
            Assert.That(result, Is.Not.Null);
            var textContent = ExtractText(result);
            Assert.That(textContent, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    public async Task Tool_With_Invalid_Parameters_Should_Return_Error()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var assemblyTool = tools.First(t => t.Name == "ReferencedAssembliesExplorer");

        // Act - Invoke with invalid parameters
        var arguments = new AIFunctionArguments
        {
            ["projectFileAbsolutePath"] = "/nonexistent/path/project.csproj",
            ["pageNumber"] = 1
        };

        var result = await assemblyTool.InvokeAsync(arguments);

        var text = ExtractText(result ?? throw new InvalidOperationException("Result is null"));
        var error = JsonSerializer.Deserialize<ToolErrorResponse>(text);
        Assert.That(error, Is.Not.Null);
        Assert.That(error!.IsError, Is.True);
        Assert.That(error.ErrorCode, Is.Not.Null.And.Not.Empty);
        Assert.That(error.ToolName, Is.EqualTo("ReferencedAssembliesExplorer"));
    }

    [Test]
    public async Task Server_Should_Handle_Concurrent_Tool_Invocations()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var searchTool = tools.First(t => t.Name == "NuGetPackageSearch");

        // Act - Invoke tools concurrently
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            var arguments = new AIFunctionArguments
            {
                ["searchQuery"] = i % 2 == 0 ? "Newtonsoft.Json" : "System.Text.Json",
                ["includePrerelease"] = false,
                ["pageNumber"] = 1
            };

            return await searchTool.InvokeAsync(arguments);
        });

        var results = await Task.WhenAll(tasks);

        // Assert - All concurrent invocations should succeed
        Assert.That(results, Has.Length.EqualTo(5));

        foreach (var result in results)
        {
            Assert.That(result, Is.Not.Null);
            var textContent = ExtractText(result);
            Assert.That(textContent, Is.Not.Null.And.Not.Empty);
        }
    }
}
