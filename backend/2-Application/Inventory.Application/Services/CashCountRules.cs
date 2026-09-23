using Common.Utilities.Exceptions;
using Inventory.Domain;

namespace Inventory.Application;

/// <summary>
/// Arqueo del cierre por medio de pago. El cajero declara sin ver lo esperado
/// (conteo a ciegas); acá se calcula lo esperado de cada medio y la diferencia.
/// </summary>
public static class CashCountRules
{
    /// <summary>
    /// Esperado, declarado y diferencia de cada medio que se arquea.
    /// </summary>
    /// <param name="cashExpected">
    /// Esperado en el cajón: fondo + ventas en efectivo + ingresos − gastos −
    /// retiros − devoluciones. Es lo que el cierre siempre calculó.
    /// </param>
    /// <param name="legacyCashDeclared">
    /// Efectivo declarado por un cliente que todavía no manda el arqueo por medio
    /// (el móvil). Solo se usa si <paramref name="counts"/> viene vacío.
    /// </param>
    /// <exception cref="CustomException">Falta declarar un medio, o hay montos inválidos.</exception>
    public static List<CashCountResponse> Compute(
        IReadOnlyList<PaymentMethod> methods, IReadOnlyList<PaymentMethodTotal> byMethod,
        decimal cashExpected, IReadOnlyList<CashCountRequest> counts, decimal legacyCashDeclared)
    {
        var arqueados = methods.Where(m => m.IsCounted).ToList();
        var efectivo = arqueados.FirstOrDefault(m => m.AffectsCash);

        if (counts.Count == 0)
        {
            // Cliente anterior: solo declara el efectivo.
            if (efectivo is null) return [];
            counts = [new CashCountRequest { PaymentMethodId = efectivo.Id, Declared = legacyCashDeclared }];
            arqueados = [efectivo];
        }

        if (counts.GroupBy(c => c.PaymentMethodId).Any(g => g.Count() > 1))
            throw new CustomException("Hay un medio de pago declarado dos veces.");
        if (counts.Any(c => c.Declared < 0))
            throw new CustomException("Los montos declarados no pueden ser negativos.");

        var faltan = arqueados.Where(m => counts.All(c => c.PaymentMethodId != m.Id)).Select(m => m.Name).ToList();
        if (faltan.Count > 0)
            throw new CustomException($"Falta declarar: {string.Join(", ", faltan)}.");

        var resultado = new List<CashCountResponse>();
        foreach (var m in arqueados)
        {
            decimal declarado = counts.First(c => c.PaymentMethodId == m.Id).Declared;
            // El efectivo usa el esperado del cajón (con fondo y movimientos); los
            // demás, el neto que entró por ese medio en el turno.
            decimal esperado = m.AffectsCash
                ? cashExpected
                : byMethod.FirstOrDefault(b => b.PaymentMethodId == m.Id)?.Net ?? 0;

            resultado.Add(new CashCountResponse
            {
                PaymentMethodId = m.Id,
                Name = m.Name,
                IconCss = m.IconCss,
                Expected = esperado,
                Declared = declarado,
                Difference = declarado - esperado,
            });
        }
        return resultado;
    }

    /// <summary>Medios cuya diferencia supera el umbral, en valor absoluto.</summary>
    public static List<string> OverThreshold(IEnumerable<CashCountResponse> counts, decimal threshold) =>
        counts.Where(c => Math.Abs(c.Difference) > threshold).Select(c => c.Name).ToList();

    /// <summary>
    /// Qué exige el cierre: observación, supervisor, o nada.
    /// </summary>
    /// <param name="priorAttempts">Cierres rechazados antes de este.</param>
    public static CloseRequirement Requirement(IReadOnlyList<CashCountResponse> counts, CashCloseSettings settings, int priorAttempts)
    {
        var conDiferencia = OverThreshold(counts, settings.NoteThreshold);
        var sobreSupervisor = settings.SupervisorThreshold is decimal tope ? OverThreshold(counts, tope) : [];
        bool agotoIntentos = settings.MaxAttempts is int max && priorAttempts >= max;

        return new CloseRequirement(
            NeedsNote: conDiferencia.Count > 0,
            NeedsSupervisor: sobreSupervisor.Count > 0 || agotoIntentos,
            OverNote: conDiferencia,
            OverSupervisor: sobreSupervisor,
            AttemptsExhausted: agotoIntentos);
    }

    /// <summary>
    /// Valida el conteo por billete y moneda contra el efectivo declarado. Devuelve
    /// las filas a guardar (sin las de cantidad cero).
    /// </summary>
    /// <param name="cashDeclared">Efectivo declarado, o null si no se arquea efectivo.</param>
    /// <exception cref="CustomException">Falta el conteo obligatorio, o no suma lo declarado.</exception>
    public static List<DenominationCount> ValidateDenominations(
        IReadOnlyList<DenominationCount> denominations, decimal? cashDeclared, bool required)
    {
        if (denominations.Count == 0)
        {
            if (required && cashDeclared is not null)
                throw new CustomException("Cuente el efectivo por billetes y monedas para poder cerrar.");
            return [];
        }

        if (cashDeclared is null)
            throw new CustomException("No hay efectivo que arquear en esta caja.");
        if (denominations.Any(d => d.Value <= 0 || d.Quantity < 0))
            throw new CustomException("El conteo de billetes y monedas tiene valores inválidos.");
        if (denominations.Any(d => decimal.Round(d.Value, 2) != d.Value))
            throw new CustomException("El valor de un billete o moneda tiene más de dos decimales.");
        if (denominations.GroupBy(d => d.Value).Any(g => g.Count() > 1))
            throw new CustomException("Hay un billete o moneda contado dos veces.");

        decimal suma = denominations.Sum(d => d.Value * d.Quantity);
        if (suma != cashDeclared)
            throw new CustomException(
                $"El conteo de billetes y monedas suma {suma:N2} y el efectivo declarado es {cashDeclared:N2}. Deben coincidir.");

        return denominations.Where(d => d.Quantity > 0).OrderByDescending(d => d.Value).ToList();
    }
}

/// <summary>Lo que un cierre necesita para pasar.</summary>
/// <param name="OverNote">Medios cuya diferencia exige observación.</param>
/// <param name="OverSupervisor">Medios cuya diferencia exige supervisor.</param>
/// <param name="AttemptsExhausted">Se llegó al máximo de intentos rechazados.</param>
public record CloseRequirement(bool NeedsNote, bool NeedsSupervisor, List<string> OverNote,
                               List<string> OverSupervisor, bool AttemptsExhausted);
