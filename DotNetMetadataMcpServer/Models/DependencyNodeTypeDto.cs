using System.Text.Json.Serialization;

namespace DotNetMetadataMcpServer.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DependencyNodeTypeDto
{
    [JsonStringEnumMemberName("root")]
    Root,

    [JsonStringEnumMemberName("target_framework")]
    TargetFramework,

    [JsonStringEnumMemberName("package")]
    Package,

    [JsonStringEnumMemberName("project")]
    Project,

    [JsonStringEnumMemberName("unknown")]
    Unknown
}
