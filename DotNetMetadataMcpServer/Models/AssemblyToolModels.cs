using System.Text.Json.Serialization;

namespace DotNetMetadataMcpServer.Models
{
    public class AssemblyToolParameters : PagedRequestWithFilter
    {
        public required string ProjectFileAbsolutePath { get; init; }
    }
    
    [JsonSerializable(typeof(AssemblyToolParameters))]
    public partial class AssemblyToolParametersJsonContext : JsonSerializerContext
    {
    }

    public class AssemblyToolResponse : PagedResponse
    {
        public IEnumerable<string> AssemblyNames { get; set; } = new List<string>();
    }
}