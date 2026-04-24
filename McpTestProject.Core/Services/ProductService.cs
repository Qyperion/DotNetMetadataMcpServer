using McpTestProject.Core.Models;

namespace McpTestProject.Core.Services;

public class ProductService : IProductService
{
    private readonly List<Product> _products = [];

    public event EventHandler<Product>? ProductCreated;

    public Product? GetById(int id)
    {
        return _products.FirstOrDefault(p => p.Id == id);
    }

    public IEnumerable<Product> GetAll()
    {
        return _products.AsReadOnly();
    }

    public void Add(Product product)
    {
        _products.Add(product);
        ProductCreated?.Invoke(this, product);
    }
}
