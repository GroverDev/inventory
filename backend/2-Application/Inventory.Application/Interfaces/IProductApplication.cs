using Common.Utilities;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Application;

public interface IProductApplication
{
    public Task<Response<string>> CreateProduct(ProductRequest productRequest, int createdBy);
    public Task<Response<bool>> UpdateProduct(ProductRequest productRequest, int modifiedBy);
    public Task<Response<bool>> DeleteProduct(string id, int modifiedBy);
    public Task<Response<List<ProductResponse>>> GetProducts(string productName);
    public Task<Response<ProductResponse>> GetProduct(string id);

    public Task<Response<ProductStockPriceResponse>> GetProductStockPrice(string id);
}
