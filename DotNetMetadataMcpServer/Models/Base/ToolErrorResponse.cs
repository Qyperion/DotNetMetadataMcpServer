using System.Text.Json.Serialization;

namespace DotNetMetadataMcpServer.Models.Base;

public class ToolErrorResponse
{
    public bool IsError { get; init; } = true;
    public required string ErrorCode { get; init; }
    public required string Message { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Details { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolName { get; init; }
}
