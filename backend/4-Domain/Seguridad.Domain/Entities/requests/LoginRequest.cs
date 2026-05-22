using FluentValidation;

namespace Seguridad.Domain;

public class LoginRequest
{
    //public string UserName { get; set; }="";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Device { get; set; } = "";
    //public bool WithEmail { get; set; }
    public Enums.InicioSesionDesde LoginFrom { get; set; } = Enums.InicioSesionDesde.Web;
    //public Accesos.InicioSesionCon LoginWith { get; set; }
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