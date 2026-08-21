using Common.Utilities;

namespace Inventory.Domain;

public class Customer : Audit
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string Cellphone { get; set; } = "";
    public bool IsActive { get; set; }

    /// <summary>
    /// Cliente genérico que el POS precarga por defecto, uno por
    /// tenant. Nunca se fija desde <c>CustomerRequest</c> (create/update no lo
    /// exponen) para que no se pueda tocar por API; solo lo siembra
    /// <c>sec.fn_seed_tenant_master_data</c>.
    /// </summary>
    public bool IsGeneric { get; set; }
}
