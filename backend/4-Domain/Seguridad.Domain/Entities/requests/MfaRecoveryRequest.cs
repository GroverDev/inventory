using FluentValidation;

namespace Seguridad.Domain;

public class MfaRecoveryRequest
{
    public string TotpSessionToken { get; set; } = "";
    public string RecoveryCode { get; set; } = "";
}

public class MfaRecoveryRequestValidator : AbstractValidator<MfaRecoveryRequest>
{
    public MfaRecoveryRequestValidator()
    {
        RuleFor(p => p.TotpSessionToken)
            .NotEmpty().WithMessage("El token de sesión MFA es requerido.");

        RuleFor(p => p.RecoveryCode)
            .NotEmpty().WithMessage("El código de recuperación es requerido.");
    }
}
