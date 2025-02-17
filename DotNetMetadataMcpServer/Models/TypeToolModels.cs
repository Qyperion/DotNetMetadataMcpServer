using System.Text.Json.Serialization;
using DotNetMetadataMcpServer.Models.Base;

namespace DotNetMetadataMcpServer.Models
{
    public class TypeToolParameters : PagedRequestWithFilter
    {
        public required string ProjectFileAbsolutePath { get; init; }
        public IEnumerable<string> Namespaces { get; set; } = new List<string>();
    }
    
    [JsonSerializable(typeof(TypeToolParameters))]
    public partial class TypeToolParametersJsonContext : JsonSerializerContext
    {
    }

    public class TypeToolResponse : PagedResponse
    {
        public IEnumerable<TypeInfoModel> TypeData { get; set; } = [];
    }
}