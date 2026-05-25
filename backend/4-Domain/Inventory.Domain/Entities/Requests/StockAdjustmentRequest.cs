using FluentValidation;

namespace Inventory.Domain;

public class StockAdjustmentRequest
{
    public string ProductId { get; set; } = "";
    public int Quantity { get; set; }         // positivo=entrada, negativo=salida
    public string Reason { get; set; } = "";
    public string Observation { get; set; } = "";
}

public class StockAdjustmentRequestValidator : AbstractValidator<StockAdjustmentRequest>
{
    public StockAdjustmentRequestValidator()
    {
        RuleFor(r => r.ProductId)
            .NotEmpty().WithMessage("El producto es requerido.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Id de producto inválido.");

        RuleFor(r => r.Quantity)
            .NotEqual(0).WithMessage("La cantidad no puede ser cero.");

        RuleFor(r => r.Reason)
            .NotEmpty().WithMessage("El motivo del ajuste es requerido.");
    }
}
