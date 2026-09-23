using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IHeldSaleRepository
{
    public Task<Guid> Hold(HeldSaleRequest request, int userId);

    /// <summary>Ventas en espera de la sucursal activa, la más vieja primero.</summary>
    public Task<List<HeldSaleResponse>> GetHeld();

    /// <summary>
    /// La saca de la espera y devuelve su carrito, o null si ya no estaba (otra
    /// caja la retomó o la descartó).
    /// </summary>
    public Task<HeldSaleResponse?> Take(Guid id);

    public Task<bool> Discard(Guid id);
}
