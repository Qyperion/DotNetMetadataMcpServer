using System.Text.Json.Serialization;

namespace DotNetMetadataMcpServer.Models
{
    public class NamespaceToolParameters : PagedRequestWithFilter
    {
        public IEnumerable<string> AssemblyNames { get; set; } = new List<string>();
    }
    
    [JsonSerializable(typeof(NamespaceToolParameters))]
    public partial class NamespaceToolParametersJsonContext : JsonSerializerContext
    {
    }

    public class NamespaceToolResponse : PagedResponse
    {
        public IEnumerable<string> Namespaces { get; set; } = new List<string>();
    }
}