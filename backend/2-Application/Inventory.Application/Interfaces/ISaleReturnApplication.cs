using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface ISaleReturnApplication
{
    Task<Response<string>> CreateReturn(SaleReturnRequest request, int createdBy);
}
