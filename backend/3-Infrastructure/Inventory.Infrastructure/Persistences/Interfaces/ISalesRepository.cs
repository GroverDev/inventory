using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ISalesRepository
{
    public Task<string> CreateSale(Sale sale);
    public Task<int> UpdateSale(Sale sale);
    public Task<int> DeleteSale(Guid id, int idUserModified);
    public Task<SaleProductResponse> GetSale(Guid Id);
    public Task<List<SaleProductResponse>> GetSales(DateTime SaleDateInitial, DateTime SaleDateEnd);
    
}
