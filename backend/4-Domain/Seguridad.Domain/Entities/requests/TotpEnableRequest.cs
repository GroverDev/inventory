using FluentValidation;

namespace Seguridad.Domain;

public class TotpEnableRequest
{
    public string Code { get; set; } = "";
}

public class TotpEnableRequestValidator : AbstractValidator<TotpEnableRequest>
{
    public TotpEnableRequestValidator()
    {
        RuleFor(p => p.Code)
            .NotEmpty().WithMessage("El código TOTP es requerido.")
            .Matches(@"^\d{6}$").WithMessage("El código TOTP debe ser exactamente 6 dígitos.");
    }
}
