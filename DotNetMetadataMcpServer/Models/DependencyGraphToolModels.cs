using System.ComponentModel.DataAnnotations;

namespace DotNetMetadataMcpServer.Models;

public class DependencyGraphToolResponse
{
    public required string ViewMode { get; init; }

    public List<DependencyGraphNodeResponse> Dependencies { get; set; } = [];

    public int TotalNodes { get; set; }
}

public class DependencyGraphNodeResponse
{
    [Required]
    public required string Name { get; init; }

    public string? Version { get; init; }

    [Required]
    public required string NodeType { get; init; }

    public int TypeCount { get; init; }

    public int Depth { get; init; }

    public string? ParentName { get; init; }

    public List<DependencyGraphNodeResponse> Children { get; init; } = [];
}
