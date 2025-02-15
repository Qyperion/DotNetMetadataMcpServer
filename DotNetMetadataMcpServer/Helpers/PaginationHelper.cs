using System.Linq;

namespace DotNetMetadataMcpServer.Helpers
{
    public static class PaginationHelper
    {
        public static List<T> Paginate<T>(IEnumerable<T> items, int pageNumber, int pageSize, out List<int> availablePages)
        {
            var totalItems = items.Count();
            var totalPages = (totalItems + pageSize - 1) / pageSize;
            availablePages = Enumerable.Range(1, totalPages).ToList();
            return items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        }
        
        public static (List<T> PaginatedItems, List<int> AvailablePages) FilterAndPaginate<T>(IEnumerable<T> items, Func<T, bool> filter, int pageNumber, int pageSize)
        {
            var filtered = items.Where(filter).ToList();
            var totalItems = filtered.Count();
            var totalPages = (totalItems + pageSize - 1) / pageSize;
            var availablePages = Enumerable.Range(1, totalPages).ToList();
            var paginated = filtered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return (paginated, availablePages);
        }
    }
}