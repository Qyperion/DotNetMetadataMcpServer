namespace DotNetMetadataMcpServer.Models;

public class PagedResponse
{
    public int CurrentPage { get; set; }
    public List<int> AvailablePages { get; set; } = [];
}