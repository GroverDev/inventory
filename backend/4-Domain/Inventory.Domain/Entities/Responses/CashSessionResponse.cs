namespace Inventory.Domain;

public class CashSessionResponse
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = "";
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsOpen => ClosedAt == null;
    public decimal OpeningAmount { get; set; }
    public decimal? DeclaredAmount { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? Difference { get; set; }
    public string Notes { get; set; } = "";
    /// <summary>Ventas de la sesión, todos los métodos de pago. Es informativo.</summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// Lo cobrado por métodos que entran al cajón (payment_methods.affects_cash),
    /// ya descontado el vuelto. Es lo único que suma al efectivo esperado: una
    /// venta por QR o tarjeta no deja plata en la caja.
    /// </summary>
    public decimal TotalCashSales { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalIncome { get; set; }

    /// <summary>
    /// Efectivo reintegrado por devoluciones en esta sesión (cash_movements de
    /// tipo return). Sale del cajón, así que resta al efectivo esperado.
    /// </summary>
    public decimal TotalReturns { get; set; }
    public List<CashMovementResponse> Movements { get; set; } = [];
}
