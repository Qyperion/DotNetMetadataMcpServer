namespace DotNetMetadataMcpServer.Configuration;

public class ToolsConfiguration
{
    public const string SectionName = "Tools";
    
    public int DefaultPageSize { get; set; } = 20;
    public bool IntendResponse { get; set; } = true;
}