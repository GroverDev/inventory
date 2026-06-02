using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Infrastructure;

public interface IProductRepository
{
    public Task<string> CreateProduct(Product product);
    public Task<int> UpdateProduct(Product product);
    public Task<int> DeleteProduct(Guid id, int idUserModified);
    public Task<List<ProductResponse>> GetProducts(string productName);
    public Task<(List<ProductResponse> Items, int TotalCount)> GetProductsStock(string productName, int page, int pageSize);
    public Task<ProductResponse> GetProduct(Guid Id);
    public Task<ProductStockPriceResponse> GetProductStockPrice(Guid Id);
    public Task<int> BulkUpdateProducts(List<ProductBulkUpdateRequest> items, int modifiedBy);
}
