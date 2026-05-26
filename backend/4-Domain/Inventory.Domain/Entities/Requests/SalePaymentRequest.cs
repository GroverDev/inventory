using FluentValidation;

namespace Inventory.Domain;

public class SalePaymentRequest
{
    public string PaymentMethodId { get; set; } = Guid.Empty.ToString();
    public string PaymentMethodName { get; set; } = "";
    public decimal AmountGiven { get; set; }
    public decimal AmountReturned { get; set; }
}

public class SalePaymentRequestValidator : AbstractValidator<SalePaymentRequest>
{
    public SalePaymentRequestValidator()
    {
        RuleFor(x => x.PaymentMethodId)
            .Must(id => Guid.TryParse(id, out var g) && g != Guid.Empty)
            .WithMessage("Método de pago no válido.");

        RuleFor(x => x.AmountGiven)
            .GreaterThan(0).WithMessage("El monto pagado debe ser mayor a cero.");
    }
}
