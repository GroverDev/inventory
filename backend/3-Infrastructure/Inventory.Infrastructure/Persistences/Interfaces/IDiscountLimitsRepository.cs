using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IDiscountLimitsRepository
{
    /// <summary>La fila de la empresa y la de la sucursal activa, las que existan.</summary>
    public Task<List<DiscountLimitLevel>> GetForCurrentBranch();

    /// <summary>La empresa y cada sucursal activa, con sus valores o vacíos.</summary>
    public Task<List<DiscountLimitLevel>> GetLevels();

    /// <summary>Guarda cada nivel; uno sin ningún valor se borra.</summary>
    public Task SaveLevels(List<DiscountLimitLevel> levels, int userId);
}
