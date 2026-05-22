using FluentValidation;

namespace Seguridad.Domain;

public class TotpSetupInitRequest
{
    public string TotpSessionToken { get; set; } = "";
}

public class TotpSetupInitRequestValidator : AbstractValidator<TotpSetupInitRequest>
{
    public TotpSetupInitRequestValidator()
    {
        RuleFor(p => p.TotpSessionToken)
            .NotEmpty().WithMessage("El token de sesión TOTP es requerido.");
    }
}
