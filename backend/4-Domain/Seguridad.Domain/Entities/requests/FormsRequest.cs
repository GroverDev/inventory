using FluentValidation;

namespace Seguridad.Domain;

public class FormsRequest
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public string NameForm { get; set; } = "";
    public string Description { get; set; } = "";
    public int Orden { get; set; }
    public string Route { get; set; } = "";
    public string Controller { get; set; } = "";
    public string IconCss { get; set; } = "";
    public bool ShowMenu { get; set; }
    public bool IsFormRegister { get; set; }
    public int ModuleId { get; set; }
}

public class FormsRequestValidator : AbstractValidator<FormsRequest>
{
    public FormsRequestValidator()
    {
        RuleFor(p => p.NameForm)
             .NotEmpty().WithMessage("El Nombre del formulario es requerido.")
             .MaximumLength(100).WithMessage("El nombre del formulario no puede ser mayor a {MaxLength} caracteres.");

        RuleFor(p => p.Description)
             .MaximumLength(250).WithMessage("La descripción no puede ser mayor a {MaxLength} caracteres.");

        RuleFor(p => p.ModuleId)
            .GreaterThan(0).WithMessage("El módulo es requerido.");
    }
}
