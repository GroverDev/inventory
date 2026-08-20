using FluentValidation;

namespace Seguridad.Domain;

public class LoginRequest
{
    //public string UserName { get; set; }="";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Device { get; set; } = "";
    //public bool WithEmail { get; set; }

    /// <summary>
    /// Origen declarado por el cliente. Sirve para la auditoría de accesos y
    /// para saber cómo entregar el refresh token (cookie o cuerpo), pero es
    /// dato no confiable: lo elige quien hace la petición. Las decisiones de
    /// seguridad no se apoyan acá.
    /// </summary>
    public Enums.InicioSesionDesde LoginFrom { get; set; } = Enums.InicioSesionDesde.Web;

    /// <summary>
    /// Token de Cloudflare Turnstile. Solo lo manda la web; el backend lo exige
    /// según la cabecera Origin, no según <see cref="LoginFrom"/>.
    /// </summary>
    public string TurnstileToken { get; set; } = "";
    //public Accesos.InicioSesionCon LoginWith { get; set; }

    /// <summary>
    /// Token de "dispositivo de confianza" emitido en una verificación TOTP
    /// anterior. Solo lo manda el móvil; en web viaja en una cookie HttpOnly y
    /// este campo se ignora. Si es válido, el login salta el paso de TOTP.
    /// </summary>
    public string DeviceTrustToken { get; set; } = "";
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(p => p.Email)
             .NotEmpty().WithMessage("El valor del nombre de Correo electrónico es requerido.")
             .EmailAddress().WithMessage("Formato de Correo Electrónico incorrecto.")
             .MinimumLength(5).WithMessage("El Correo electrónico no puede ser menor a {MinLength} caracteres. ")
             .MaximumLength(50).WithMessage("El Correo electrónico no puede ser mayor a {MaximumLength} caracteres. ");


        RuleFor(p => p.Password)
             .NotEmpty().WithMessage("El valor del password es requerido.")
             .MinimumLength(3).WithMessage("El {PropertyName} no puede ser menor a {MinLength} caracteres.")
             .MaximumLength(50).WithMessage("El {PropertyName} no puede ser mayor a {MaximumLength} caracteres. ");

        // RuleFor(p => p.Device)
        //      .NotEmpty().WithMessage("El valor del {PropertyName} es requerido.");

    }
}