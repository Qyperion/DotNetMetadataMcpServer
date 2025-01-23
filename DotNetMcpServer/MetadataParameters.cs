using System.Text.Json.Serialization;

namespace MySolution.McpServer.Handlers;

public class MetadataParameters
{
    public required string ProjectFilePath { get; init; }
}

[JsonSerializable(typeof(MetadataParameters))]
public partial class MetadataParametersJsonContext : JsonSerializerContext
{
}