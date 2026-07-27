using FluentValidation;

namespace Seguridad.Domain;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = "";
    public string Device { get; set; } = "";
    public Enums.InicioSesionDesde LoginFrom { get; set; } = Enums.InicioSesionDesde.ReconexionMovil;
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(p => p.RefreshToken)
            .NotEmpty().WithMessage("El refresh token es requerido.")
            .MaximumLength(200).WithMessage("Refresh token inválido.");
    }
}
