namespace DotNetMetadataMcpServer.Models.Base;

public class PagedResponse
{
    public int CurrentPage { get; set; }
    public List<int> AvailablePages { get; set; } = [];
}