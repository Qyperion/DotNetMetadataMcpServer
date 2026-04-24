using McpTestProject.Core.Models;

namespace McpTestProject.Core.Services;

public interface IProductService
{
    Product? GetById(int id);
    IEnumerable<Product> GetAll();
}
