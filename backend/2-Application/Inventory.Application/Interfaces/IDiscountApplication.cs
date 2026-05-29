using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application.Interfaces;

public interface IDiscountApplication
{
    Task<Response<List<DiscountResponse>>> GetDiscounts();
    Task<Response<DiscountResponse>> GetDiscount(string id);
    Task<Response<string>> CreateDiscount(DiscountRequest request, int createdBy);
    Task<Response<bool>> UpdateDiscount(string id, DiscountRequest request, int modifiedBy);
    Task<Response<bool>> DeleteDiscount(string id, int modifiedBy);
}
