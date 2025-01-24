using System.Text.Json.Serialization;

namespace DotNetMetadataMcpServer;

public class MetadataParameters
{
    public required string ProjectFileAbsolutePath { get; init; }
}

[JsonSerializable(typeof(MetadataParameters))]
public partial class MetadataParametersJsonContext : JsonSerializerContext
{
}