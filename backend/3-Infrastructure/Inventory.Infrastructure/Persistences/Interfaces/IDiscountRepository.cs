using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IDiscountRepository
{
    Task<List<DiscountResponse>> GetDiscounts();
    Task<DiscountResponse?> GetDiscount(Guid id);
    Task<string> CreateDiscount(Discount discount);
    Task<int> UpdateDiscount(Discount discount);
    Task<int> DeleteDiscount(Guid id, int modifiedBy);
}
