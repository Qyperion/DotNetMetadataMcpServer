using DotNetMetadataMcpServer.Models.Base;

namespace DotNetMetadataMcpServer.Models;

public class NamespaceToolResponse : PagedResponse
{
    public IEnumerable<string> Namespaces { get; set; } = [];
}