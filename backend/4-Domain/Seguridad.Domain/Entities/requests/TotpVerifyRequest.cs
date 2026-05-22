using FluentValidation;

namespace Seguridad.Domain;

public class TotpVerifyRequest
{
    public string TotpSessionToken { get; set; } = "";
    public string TotpCode { get; set; } = "";
    public string Device { get; set; } = "";
    public Enums.InicioSesionDesde LoginFrom { get; set; } = Enums.InicioSesionDesde.Web;
}

public class TotpVerifyRequestValidator : AbstractValidator<TotpVerifyRequest>
{
    public TotpVerifyRequestValidator()
    {
        RuleFor(p => p.TotpSessionToken)
            .NotEmpty().WithMessage("El token de sesión TOTP es requerido.");

        RuleFor(p => p.TotpCode)
            .NotEmpty().WithMessage("El código TOTP es requerido.")
            .Matches(@"^\d{6}$").WithMessage("El código TOTP debe ser exactamente 6 dígitos.");
    }
}
