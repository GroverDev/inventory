using System.Data;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ISaleReturnRepository
{
    Task<string> CreateReturn(SaleReturn saleReturn);
    Task<List<SaleReturnResponse>> GetReturnsBySale(Guid saleId, IDbConnection db);
}
