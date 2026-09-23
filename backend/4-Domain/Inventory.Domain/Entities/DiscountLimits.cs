using FluentValidation;

namespace Inventory.Domain;

/// <summary>
/// Topes del descuento manual de un nivel: la empresa (<see cref="BranchId"/>
/// nulo) o una sucursal. Un valor nulo hereda del nivel superior.
/// </summary>
public class DiscountLimitLevel
{
    public Guid? BranchId { get; set; }

    /// <summary>Solo lectura: nombre de la sucursal, o vacío para la empresa.</summary>
    public string BranchName { get; set; } = "";

    /// <summary>Tope del cajero: por encima pide autorización de supervisor.</summary>
    public decimal? CashierMaxPct { get; set; }
    public decimal? CashierMaxAmount { get; set; }

    /// <summary>Tope máximo para cualquier rol: nadie lo supera, ni con autorización.</summary>
    public decimal? GeneralMaxPct { get; set; }
    public decimal? GeneralMaxAmount { get; set; }

    public bool IsEmpty => CashierMaxPct is null && CashierMaxAmount is null
                        && GeneralMaxPct is null && GeneralMaxAmount is null;
}

/// <summary>Topes vigentes en una sucursal, ya resueltos entre sucursal, empresa y default.</summary>
public class EffectiveDiscountLimits
{
    public decimal CashierMaxPct { get; set; }
    public decimal CashierMaxAmount { get; set; }

    /// <summary>Null = sin tope máximo.</summary>
    public decimal? GeneralMaxPct { get; set; }
    public decimal? GeneralMaxAmount { get; set; }
}

public class DiscountLimitLevelValidator : AbstractValidator<List<DiscountLimitLevel>>
{
    public DiscountLimitLevelValidator()
    {
        RuleForEach(l => l).ChildRules(x =>
        {
            x.RuleFor(n => n.CashierMaxPct).InclusiveBetween(0, 100).When(n => n.CashierMaxPct.HasValue)
                .WithMessage("Los porcentajes van de 0 a 100.");
            x.RuleFor(n => n.GeneralMaxPct).InclusiveBetween(0, 100).When(n => n.GeneralMaxPct.HasValue)
                .WithMessage("Los porcentajes van de 0 a 100.");
            x.RuleFor(n => n.CashierMaxAmount).GreaterThanOrEqualTo(0).When(n => n.CashierMaxAmount.HasValue)
                .WithMessage("Los montos no pueden ser negativos.");
            x.RuleFor(n => n.GeneralMaxAmount).GreaterThanOrEqualTo(0).When(n => n.GeneralMaxAmount.HasValue)
                .WithMessage("Los montos no pueden ser negativos.");
        });
    }
}
