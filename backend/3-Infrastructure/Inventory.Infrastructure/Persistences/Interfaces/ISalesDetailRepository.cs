using System.Data;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ISalesDetailRepository
{
    public Task<bool> CreateSaleDetail(SaleDetail provider, IDbConnection db,  IDbTransaction transaction);
    public Task<List<SaleProductDetailResponse>> GetSalesProductDetail(Guid idSale, IDbConnection db);
}
