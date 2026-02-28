using DotNetMetadataMcpServer.Models.Base;
using System.Text.Json;

namespace DotNetMetadataMcpServer.Helpers;

public static class ToolResultHelper
{
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    public static string Serialize<T>(T value, bool indent)
    {
        return indent
            ? JsonSerializer.Serialize(value, IndentedOptions)
            : JsonSerializer.Serialize(value);
    }

    public static string SerializeError(Exception exception, bool indent, string toolName)
    {
        var error = new ToolErrorResponse
        {
            ErrorCode = MapErrorCode(exception),
            Message = "Tool execution failed.",
            Details = exception.Message,
            ToolName = toolName
        };

        return Serialize(error, indent);
    }

    private static string MapErrorCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "invalid_argument",
            InvalidOperationException => "invalid_operation",
            FileNotFoundException => "file_not_found",
            TimeoutException => "timeout",
            _ => "internal_error"
        };
    }
}
