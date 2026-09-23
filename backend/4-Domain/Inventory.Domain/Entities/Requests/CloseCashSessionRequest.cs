namespace Inventory.Domain;

public class CloseCashSessionRequest
{
    /// <summary>
    /// Efectivo contado. Lo usan los clientes anteriores al arqueo por medio (el
    /// móvil); si llega <see cref="Counts"/>, se ignora.
    /// </summary>
    public decimal DeclaredAmount { get; set; }
    public string Notes { get; set; } = "";

    /// <summary>Lo declarado por cada medio que se arquea, sin haber visto lo esperado.</summary>
    public List<CashCountRequest> Counts { get; set; } = [];

    /// <summary>
    /// El efectivo contado por billete y moneda. Si viene, tiene que sumar lo
    /// declarado en efectivo; es obligatorio si la empresa lo configuró.
    /// </summary>
    public List<DenominationCount> Denominations { get; set; } = [];

    /// <summary>
    /// Sesión de un supervisor que autoriza el cierre, cuando la diferencia o los
    /// intentos lo exigen.
    /// </summary>
    public string? SupervisorAuthToken { get; set; }
}

public class CashCountRequest
{
    public Guid PaymentMethodId { get; set; }
    public decimal Declared { get; set; }
}
