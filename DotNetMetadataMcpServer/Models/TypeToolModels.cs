using System.Text.Json.Serialization;

namespace DotNetMetadataMcpServer.Models
{
    public class TypeToolParameters : PagedRequestWithFilter
    {
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