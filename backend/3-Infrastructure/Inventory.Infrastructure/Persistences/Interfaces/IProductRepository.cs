using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Infrastructure;

public interface IProductRepository
{
    public Task<string> CreateProduct(Product product);
    public Task<int> UpdateProduct(Product product);
    public Task<int> DeleteProduct(Guid id, int idUserModified);
    /// <param name="branches">Sucursales cuyo stock se suma; null = la sucursal activa.</param>
    public Task<List<ProductResponse>> GetProducts(string productName, Guid[]? branches = null);
    public Task<(List<ProductResponse> Items, int TotalCount)> GetProductsStock(string productName, int page, int pageSize);
    public Task<ProductResponse> GetProduct(Guid Id);
    public Task<ProductStockPriceResponse> GetProductStockPrice(Guid Id);
    public Task<(bool Found, string? PreviousPath)> SetImagePath(Guid id, string? path, int modifiedBy);
    public Task<int> BulkUpdateProducts(List<ProductBulkUpdateRequest> items, int modifiedBy);
    public Task ActivateTracking(Guid id, string modo);
}
