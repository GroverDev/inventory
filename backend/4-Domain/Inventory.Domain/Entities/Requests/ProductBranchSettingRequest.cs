using FluentValidation;

namespace Inventory.Domain;

/// <summary>
/// Excepción de una sucursal. Con los dos valores nulos, la sucursal vuelve a
/// usar los valores base.
/// </summary>
public class ProductBranchSettingRequest
{
    public Guid BranchId { get; set; }
    public decimal? SalePrice { get; set; }
    public int? MinReorderQuantity { get; set; }
}

public class ProductBranchSettingRequestValidator : AbstractValidator<List<ProductBranchSettingRequest>>
{
    public ProductBranchSettingRequestValidator()
    {
        RuleFor(l => l)
            .Must(l => l.Select(x => x.BranchId).Distinct().Count() == l.Count)
            .WithMessage("Hay sucursales repetidas.");
        RuleForEach(l => l).ChildRules(x =>
        {
            x.RuleFor(s => s.BranchId).NotEmpty().WithMessage("Hay una fila sin sucursal.");
            x.RuleFor(s => s.SalePrice).GreaterThanOrEqualTo(0).When(s => s.SalePrice.HasValue)
                .WithMessage("El precio no puede ser negativo.");
            x.RuleFor(s => s.MinReorderQuantity).GreaterThanOrEqualTo(0).When(s => s.MinReorderQuantity.HasValue)
                .WithMessage("El mínimo de reposición no puede ser negativo.");
        });
    }
}
