using FluentValidation;

namespace Seguridad.Domain;

public class RolesFormsRequest
{
    public int RolId { get; set; }
    public List<int> FormIds { get; set; } = [];
}

public class RolesFormsRequestValidator : AbstractValidator<RolesFormsRequest>
{
    public RolesFormsRequestValidator()
    {
        RuleFor(r => r.RolId)
            .GreaterThan(0).WithMessage("El rol es requerido.");
    }
}
