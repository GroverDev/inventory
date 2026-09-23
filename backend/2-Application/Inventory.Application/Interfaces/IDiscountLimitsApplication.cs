using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface IDiscountLimitsApplication
{
    /// <summary>Topes vigentes en la sucursal activa.</summary>
    public Task<EffectiveDiscountLimits> GetEffective();

    public Task<Response<List<DiscountLimitLevel>>> GetLevels();
    public Task<Response<bool>> SaveLevels(List<DiscountLimitLevel> levels, int userId);
}
