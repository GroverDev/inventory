using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ISalesRepository
{
    public Task<string> CreateSale(Sale sale);
    public Task<int> UpdateSale(Sale sale);
    public Task<int> DeleteSale(Guid id, int idUserModified);
    public Task<SaleProductResponse> GetSale(Guid Id);
    public Task<SalesPagedResponse> GetSales(DateTime saleDateInitial, DateTime saleDateEnd, int? userId = null, int page = 1, int pageSize = 50, string? sellerName = null);
    
}
