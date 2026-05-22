using FluentValidation;

namespace Seguridad.Domain;

public class ModulesRequest
{
    public int Id { get; set; }
    public string NameModule { get; set; } = "";
    public int ShowOrder { get; set; }
    public string Route { get; set; } = "";
    public string IconCss { get; set; } = "";
}

public class ModulesRequestValidator : AbstractValidator<ModulesRequest>
{
    public ModulesRequestValidator()
    {
        RuleFor(p => p.NameModule)
             .NotEmpty().WithMessage("El Nombre del módulo es requerido.")
             .MaximumLength(100).WithMessage("El nombre del módulo no puede ser mayor a {MaxLength} caracteres.");

        RuleFor(p => p.Route)
             .MaximumLength(200).WithMessage("La ruta no puede ser mayor a {MaxLength} caracteres.");
    }
}
