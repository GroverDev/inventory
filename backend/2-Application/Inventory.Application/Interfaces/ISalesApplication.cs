using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface ISalesApplication
{
    public Task<Response<string>> CreateSale(SaleRequest saleRequest, int createdBy);
    public Task<Response<bool>> UpdateSale(SaleRequest saleRequest, int modifiedBy);
    public Task<Response<bool>> DeleteSale(string id, int modifiedBy);
     public Task<Response<List<SaleProductResponse>>> GetSales(string saleDateInitial, string saleDateEnd);
      public  Task<Response<SaleProductResponse>> GetSale(string id);
    
}
