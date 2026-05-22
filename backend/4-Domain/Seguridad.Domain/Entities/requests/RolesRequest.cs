using FluentValidation;

namespace Seguridad.Domain;

public class RolesRequest
{
    public int Id { get; set; }
    public string NameRol { get; set; } = "";
    public string Description { get; set; } = "";
}

public class RolesRequestValidator : AbstractValidator<RolesRequest>
{
    public RolesRequestValidator()
    {
        RuleFor(r => r.NameRol)
            .NotEmpty().WithMessage("El nombre del rol es requerido.")
            .MaximumLength(100).WithMessage("El nombre del rol no puede exceder 100 caracteres.");

        RuleFor(r => r.Description)
            .MaximumLength(250).WithMessage("La descripción no puede exceder 250 caracteres.");
    }
}
