using Common.Utilities;

namespace Inventory.Domain;

/// <summary>Sucursal de la farmacia (public.branches).</summary>
public class Branch : Audit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public bool IsActive { get; set; }

    /// <summary>Usuarios activos habilitados en la sucursal. Solo lectura, para el listado.</summary>
    public int UsersCount { get; set; }
}
