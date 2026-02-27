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
/// End-to-end integration tests for NuGetPackageSearch and NuGetPackageVersions tools
/// using a real MCP server/client with in-memory pipes against the live NuGet API.
/// </summary>
[TestFixture]
[NonParallelizable] // MCP client with pipe streams does not support concurrent reads/writes
public class NuGetToolsEndToEndTests : McpServerIntegrationTestBase
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
            .WithTools<NuGetTools>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tools:DefaultPageSize"] = "20",
                ["Tools:IndentResponse"] = "false"
            })
            .Build();

        services.Configure<ToolsConfiguration>(configuration.GetSection("Tools"));
        services.AddScoped<NuGetToolService>();
    }

    #region NuGetPackageSearch Tests

    [Test]
    public async Task NuGetPackageSearch_Should_Return_Results_With_Package_Details()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageSearch");

        var arguments = new AIFunctionArguments
        {
            ["searchQuery"] = "Newtonsoft.Json",
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageSearchResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Packages, Is.Not.Empty);
        Assert.That(response.CurrentPage, Is.EqualTo(1));
        Assert.That(response.AvailablePages, Is.Not.Empty);

        // Verify the first matching package has all expected fields populated
        var newtonsoftPkg = response.Packages.First(p =>
            p.Id.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase));
        Assert.That(newtonsoftPkg.Version, Is.Not.Null.And.Not.Empty, "Package should have a Version");
        Assert.That(newtonsoftPkg.Description, Is.Not.Null.And.Not.Empty, "Package should have a Description");
        Assert.That(newtonsoftPkg.Authors, Is.Not.Null.And.Not.Empty, "Package should have Authors");
        Assert.That(newtonsoftPkg.DownloadCount, Is.GreaterThan(0), "Newtonsoft.Json should have downloads");
    }

    [Test]
    public async Task NuGetPackageSearch_WithFilter_Should_Return_Only_Matching()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageSearch");

        var arguments = new AIFunctionArguments
        {
            ["searchQuery"] = "Json",
            ["fullTextFiltersWithWildCardSupport"] = new[] { "Newtonsoft*" },
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageSearchResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Packages, Is.Not.Empty);

        // All returned packages should match the filter (Id or Description contains "Newtonsoft")
        foreach (var pkg in response.Packages)
        {
            var matchesId = pkg.Id.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase);
            var matchesDescription = pkg.Description?.Contains("Newtonsoft", StringComparison.OrdinalIgnoreCase) ?? false;
            Assert.That(matchesId || matchesDescription, Is.True,
                $"Package '{pkg.Id}' should match filter 'Newtonsoft*' in Id or Description");
        }
    }

    [Test]
    public async Task NuGetPackageSearch_WithNonMatchingFilter_Should_Return_Empty()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageSearch");

        var arguments = new AIFunctionArguments
        {
            ["searchQuery"] = "Newtonsoft.Json",
            ["fullTextFiltersWithWildCardSupport"] = new[] { "ZzzNonExistentFilter12345*" },
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageSearchResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Packages, Is.Empty,
            "Non-matching filter should return no packages");
    }

    [Test]
    public async Task NuGetPackageSearch_WithPrerelease_Should_Include_Results()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageSearch");

        var arguments = new AIFunctionArguments
        {
            ["searchQuery"] = "Newtonsoft.Json",
            ["includePrerelease"] = true,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageSearchResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Packages, Is.Not.Empty,
            "Search with includePrerelease=true should still return results");
    }

    [Test]
    public async Task NuGetPackageSearch_Pagination_Should_Return_Correct_Page()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageSearch");

        // Get page 1 first to confirm there are results
        var page1Args = new AIFunctionArguments
        {
            ["searchQuery"] = "Microsoft.Extensions",
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };
        var page1Result = await tool.InvokeAsync(page1Args);
        var page1Response = JsonSerializer.Deserialize<NuGetPackageSearchResponse>(ExtractText(page1Result));
        Assert.That(page1Response, Is.Not.Null);
        Assert.That(page1Response!.Packages, Is.Not.Empty, "Page 1 should have results");

        // Skip page 2 test if only 1 page available
        if (page1Response.AvailablePages.Count <= 1)
        {
            Assert.Pass("Only 1 page of results; pagination test not applicable");
            return;
        }

        // Act - Get page 2
        var page2Args = new AIFunctionArguments
        {
            ["searchQuery"] = "Microsoft.Extensions",
            ["includePrerelease"] = false,
            ["pageNumber"] = 2
        };
        var page2Result = await tool.InvokeAsync(page2Args);

        // Assert
        var page2Response = JsonSerializer.Deserialize<NuGetPackageSearchResponse>(ExtractText(page2Result));
        Assert.That(page2Response, Is.Not.Null);
        Assert.That(page2Response!.CurrentPage, Is.EqualTo(2));
        Assert.That(page2Response.Packages, Is.Not.Empty, "Page 2 should have results");

        // Pages should contain different packages
        var page1Ids = page1Response.Packages.Select(p => p.Id).ToHashSet();
        var page2Ids = page2Response.Packages.Select(p => p.Id).ToHashSet();
        Assert.That(page1Ids.Overlaps(page2Ids), Is.False,
            "Page 1 and Page 2 should have different packages");
    }

    [Test]
    public async Task NuGetPackageSearch_WithInvalidPageNumber_Should_Return_Empty()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageSearch");

        var arguments = new AIFunctionArguments
        {
            ["searchQuery"] = "Newtonsoft.Json",
            ["includePrerelease"] = false,
            ["pageNumber"] = 999
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageSearchResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Packages, Is.Empty,
            "An out-of-range page number should return empty results");
    }

    #endregion

    #region NuGetPackageVersions Tests

    [Test]
    public async Task NuGetPackageVersions_Should_Return_Versions_With_Details()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageVersions");

        var arguments = new AIFunctionArguments
        {
            ["packageId"] = "Newtonsoft.Json",
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageVersionsResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.PackageId, Is.EqualTo("Newtonsoft.Json"));
        Assert.That(response.Versions, Is.Not.Empty);
        Assert.That(response.CurrentPage, Is.EqualTo(1));
        Assert.That(response.AvailablePages, Is.Not.Empty);

        // Verify version entries have expected fields
        var version = response.Versions.First();
        Assert.That(version.Id, Is.EqualTo("Newtonsoft.Json").IgnoreCase);
        Assert.That(version.Version, Is.Not.Null.And.Not.Empty, "Version should have a version string");
        Assert.That(version.Description, Is.Not.Null.And.Not.Empty, "Version should have a description");
    }

    [Test]
    public async Task NuGetPackageVersions_WithFilter_Should_Return_Only_Matching_Versions()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageVersions");

        var arguments = new AIFunctionArguments
        {
            ["packageId"] = "Newtonsoft.Json",
            ["fullTextFiltersWithWildCardSupport"] = new[] { "13.*" },
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageVersionsResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Versions, Is.Not.Empty);

        // All returned versions should match the filter pattern "13.*"
        foreach (var v in response.Versions)
        {
            Assert.That(v.Version, Does.StartWith("13."),
                $"Version '{v.Version}' does not match filter '13.*'");
        }
    }

    [Test]
    public async Task NuGetPackageVersions_WithDependencies_Should_Include_DependencyGroups()
    {
        // Arrange - Use a package known to have dependencies
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageVersions");

        var arguments = new AIFunctionArguments
        {
            ["packageId"] = "Microsoft.Extensions.DependencyInjection",
            ["fullTextFiltersWithWildCardSupport"] = new[] { "8.0.0" },
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageVersionsResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Versions, Is.Not.Empty);

        var version = response.Versions.First(v => v.Version == "8.0.0");
        Assert.That(version.DependencyGroups, Is.Not.Empty,
            "Microsoft.Extensions.DependencyInjection 8.0.0 should have dependency groups");

        // Verify dependency group structure
        var group = version.DependencyGroups.First();
        Assert.That(group.TargetFramework, Is.Not.Null.And.Not.Empty,
            "Dependency group should have a TargetFramework");
    }

    [Test]
    public async Task NuGetPackageVersions_ForNonExistentPackage_Should_Return_Empty_Versions()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageVersions");

        var arguments = new AIFunctionArguments
        {
            ["packageId"] = "ZzzThisPackageDoesNotExist12345XYZ",
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageVersionsResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Versions, Is.Empty,
            "Nonexistent package should return empty versions");
    }

    [Test]
    public async Task NuGetPackageVersions_WithPrerelease_Should_Return_Results()
    {
        // Arrange
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageVersions");

        var arguments = new AIFunctionArguments
        {
            ["packageId"] = "Newtonsoft.Json",
            ["includePrerelease"] = true,
            ["pageNumber"] = 1
        };

        // Act
        var result = await tool.InvokeAsync(arguments);

        // Assert
        var text = ExtractText(result);
        var response = JsonSerializer.Deserialize<NuGetPackageVersionsResponse>(text);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Versions, Is.Not.Empty,
            "Getting versions with includePrerelease=true should return results");
    }

    [Test]
    public async Task NuGetPackageVersions_Pagination_Should_Return_Correct_Page()
    {
        // Arrange - Newtonsoft.Json has many versions, should paginate
        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var tool = tools.First(t => t.Name == "NuGetPackageVersions");

        // Get page 1
        var page1Args = new AIFunctionArguments
        {
            ["packageId"] = "Newtonsoft.Json",
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        };
        var page1Result = await tool.InvokeAsync(page1Args);
        var page1Response = JsonSerializer.Deserialize<NuGetPackageVersionsResponse>(ExtractText(page1Result));
        Assert.That(page1Response, Is.Not.Null);
        Assert.That(page1Response!.Versions, Is.Not.Empty, "Page 1 should have versions");

        if (page1Response.AvailablePages.Count <= 1)
        {
            Assert.Pass("Only 1 page of versions; pagination test not applicable");
            return;
        }

        // Act - Get page 2
        var page2Args = new AIFunctionArguments
        {
            ["packageId"] = "Newtonsoft.Json",
            ["includePrerelease"] = false,
            ["pageNumber"] = 2
        };
        var page2Result = await tool.InvokeAsync(page2Args);

        // Assert
        var page2Response = JsonSerializer.Deserialize<NuGetPackageVersionsResponse>(ExtractText(page2Result));
        Assert.That(page2Response, Is.Not.Null);
        Assert.That(page2Response!.CurrentPage, Is.EqualTo(2));
        Assert.That(page2Response.Versions, Is.Not.Empty, "Page 2 should have versions");

        // Pages should contain different versions
        var page1Versions = page1Response.Versions.Select(v => v.Version).ToHashSet();
        var page2Versions = page2Response.Versions.Select(v => v.Version).ToHashSet();
        Assert.That(page1Versions.Overlaps(page2Versions), Is.False,
            "Page 1 and Page 2 should have different versions");
    }

    #endregion

    #region Cross-Tool Workflow Tests

    [Test]
    public async Task FullWorkflow_SearchPackage_Then_GetVersions()
    {
        // This test simulates a real workflow:
        // 1. Search for a package → 2. Get its version history

        await using var client = await CreateMcpClientAsync();
        var tools = await client.ListToolsAsync();

        // Step 1: Search for a package
        var searchTool = tools.First(t => t.Name == "NuGetPackageSearch");
        var searchResult = await searchTool.InvokeAsync(new AIFunctionArguments
        {
            ["searchQuery"] = "FluentValidation",
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        });
        var searchResponse = JsonSerializer.Deserialize<NuGetPackageSearchResponse>(ExtractText(searchResult));
        Assert.That(searchResponse, Is.Not.Null);
        Assert.That(searchResponse!.Packages, Is.Not.Empty, "Step 1: Should find packages");

        // Get the package ID from search results
        var foundPackage = searchResponse.Packages.First(p =>
            p.Id.Equals("FluentValidation", StringComparison.OrdinalIgnoreCase));
        Assert.That(foundPackage, Is.Not.Null, "Should find FluentValidation in search results");

        // Step 2: Get versions for the found package
        var versionsTool = tools.First(t => t.Name == "NuGetPackageVersions");
        var versionsResult = await versionsTool.InvokeAsync(new AIFunctionArguments
        {
            ["packageId"] = foundPackage.Id,
            ["includePrerelease"] = false,
            ["pageNumber"] = 1
        });
        var versionsResponse = JsonSerializer.Deserialize<NuGetPackageVersionsResponse>(ExtractText(versionsResult));
        Assert.That(versionsResponse, Is.Not.Null);
        Assert.That(versionsResponse!.PackageId, Is.EqualTo(foundPackage.Id).IgnoreCase);
        Assert.That(versionsResponse.Versions, Is.Not.Empty,
            "Step 2: Should have version history for the found package");

        // Verify the workflow connected correctly: package ID matches and versions are available
        // Note: The exact version from search may not be on page 1 of versions due to pagination ordering
        Assert.That(versionsResponse.Versions.All(v =>
            v.Id.Equals(foundPackage.Id, StringComparison.OrdinalIgnoreCase)),
            Is.True, "All returned versions should belong to the searched package");
    }

    #endregion
}
