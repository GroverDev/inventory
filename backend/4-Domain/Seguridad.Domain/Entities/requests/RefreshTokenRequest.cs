using FluentValidation;

namespace Seguridad.Domain;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = "";
    public string Device { get; set; } = "";
    public Enums.InicioSesionDesde LoginFrom { get; set; } = Enums.InicioSesionDesde.ReconexionMovil;

    /// <summary>
    /// Solo lo lee el cierre de sesión (<c>Login/revoke</c>). Si es true se
    /// revoca únicamente el refresh token y el equipo sigue siendo de confianza:
    /// lo usa el cierre por inactividad, que no es una decisión del usuario. El
    /// botón "Cerrar sesión" no lo manda, y ahí sí se olvida el equipo.
    /// </summary>
    public bool KeepDevice { get; set; }
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
