namespace DotNetMetadataMcpServer.Models.Base;

public abstract class PagedRequestWithFilter
{
    public int PageNumber { get; set; } = 1;
    public List<string> FullTextFiltersWithWildCardSupport { get; set; } = [];
}