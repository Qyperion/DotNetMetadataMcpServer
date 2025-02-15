using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotNetMetadataMcpServer.Models
{
    public class NamespaceToolParameters : PagedRequestWithFilter
    {
        [Description("The absolute path to the project file.")]
        public required string ProjectFileAbsolutePath { get; init; }
        
        [Description("The assembly names to filter by (without exe\\dll extension). If empty, all assemblies are considered.")]
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