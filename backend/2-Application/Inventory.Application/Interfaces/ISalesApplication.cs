using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface ISalesApplication
{
    public Task<Response<string>> CreateSale(SaleRequest saleRequest, int createdBy, string userRole, bool supervisorApproved = false);
    public Task<Response<bool>> UpdateSale(SaleRequest saleRequest, int modifiedBy);
    public Task<Response<bool>> DeleteSale(string id, int modifiedBy);
    public Task<Response<SalesPagedResponse>> GetSales(string saleDateInitial, string saleDateEnd, int userId, string rol, int page = 1, int pageSize = 50, string? sellerName = null);
      public  Task<Response<SaleProductResponse>> GetSale(string id);
    
}
