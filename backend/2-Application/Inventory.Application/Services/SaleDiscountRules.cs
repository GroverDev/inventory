using Common.Utilities.Exceptions;
using Inventory.Domain;

namespace Inventory.Application;

/// <summary>
/// Cálculo y topes del descuento manual. El servidor calcula el importe desde el
/// tipo y el valor: nunca toma el importe que manda el cliente, que podría no
/// corresponder al valor que se validó contra los topes.
/// </summary>
public static class SaleDiscountRules
{
    public const string Percentage = "Percentage";
    public const string FixedAmount = "FixedAmount";

    /// <summary>
    /// Importe de un descuento manual sobre <paramref name="baseAmount"/>. Cero si
    /// no hay descuento; nunca mayor que la base.
    /// </summary>
    /// <exception cref="CustomException">Tipo desconocido o valor fuera de rango.</exception>
    public static decimal ManualAmount(string? type, decimal value, decimal baseAmount)
    {
        if (value == 0) return 0;
        if (value < 0) throw new CustomException("El descuento no puede ser negativo.");

        return type switch
        {
            Percentage when value > 100 =>
                throw new CustomException("Un descuento por porcentaje no puede superar el 100%."),
            Percentage => Math.Round(baseAmount * value / 100, 2),
            FixedAmount => Math.Min(value, baseAmount),
            _ => throw new CustomException("El descuento manual no indica un tipo válido (porcentaje o monto)."),
        };
    }

    /// <summary>Verifica un descuento manual contra los topes vigentes.</summary>
    /// <param name="cajeroSinAutorizacion">
    /// Aplica el tope del cajero: solo si su único rol es Cajero y ningún
    /// supervisor autorizó. El tope máximo aplica siempre.
    /// </param>
    /// <param name="donde">"por línea" o "global", para el mensaje.</param>
    public static void CheckLimits(string? type, decimal value, EffectiveDiscountLimits limits,
                                   bool cajeroSinAutorizacion, string donde)
    {
        if (value == 0) return;

        if (type == Percentage)
        {
            if (limits.GeneralMaxPct is decimal maxPct && value > maxPct)
                throw new CustomException(
                    $"Descuento {donde} por porcentaje supera el tope máximo del {maxPct:0.##}% de esta sucursal.");
            if (cajeroSinAutorizacion && value > limits.CashierMaxPct)
                throw new CustomException(
                    $"Descuento manual {donde} por porcentaje supera el límite del {limits.CashierMaxPct:0.##}% permitido para cajeros.");
        }
        else if (type == FixedAmount)
        {
            if (limits.GeneralMaxAmount is decimal maxMonto && value > maxMonto)
                throw new CustomException(
                    $"Descuento {donde} por monto supera el tope máximo de Bs. {maxMonto:0.00} de esta sucursal.");
            if (cajeroSinAutorizacion && value > limits.CashierMaxAmount)
                throw new CustomException(
                    $"Descuento manual {donde} por monto supera el límite de Bs. {limits.CashierMaxAmount:0.00} permitido para cajeros.");
        }
    }

    /// <summary>
    /// Resuelve cada tope por separado: el de la sucursal si lo tiene, si no el
    /// de la empresa, si no el default (appsettings para el cajero, sin tope
    /// para el máximo).
    /// </summary>
    public static EffectiveDiscountLimits Resolve(DiscountLimitLevel? empresa, DiscountLimitLevel? sucursal, PosSettings defaults) => new()
    {
        CashierMaxPct    = sucursal?.CashierMaxPct    ?? empresa?.CashierMaxPct    ?? defaults.MaxCashierDiscountPct,
        CashierMaxAmount = sucursal?.CashierMaxAmount ?? empresa?.CashierMaxAmount ?? defaults.MaxCashierDiscountAmount,
        GeneralMaxPct    = sucursal?.GeneralMaxPct    ?? empresa?.GeneralMaxPct,
        GeneralMaxAmount = sucursal?.GeneralMaxAmount ?? empresa?.GeneralMaxAmount,
    };
}
