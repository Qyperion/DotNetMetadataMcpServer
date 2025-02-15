
using System.Collections.Generic;
using System.Linq;

namespace DotNetMetadataMcpServer.Helpers
{
    public static class PaginationService
    {
        public static IEnumerable<T> Paginate<T>(IEnumerable<T> source, int pageNumber, int pageSize)
        {
            return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
    }
}