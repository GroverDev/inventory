using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface IHeldSaleApplication
{
    public Task<Response<string>> Hold(HeldSaleRequest request, int userId);
    public Task<Response<List<HeldSaleResponse>>> GetHeld();
    public Task<Response<HeldSaleResponse>> Take(Guid id);
    public Task<Response<bool>> Discard(Guid id);
}
