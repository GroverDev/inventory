using FluentValidation;

namespace Seguridad.Domain;

public class RolSearchRequest
{
    public string NameRol { get; set; }="";
    public string Description { get; set; }="";
}
public class RolSearchRequestValidator : AbstractValidator<RolSearchRequest>
{
    public RolSearchRequestValidator()
    {
        RuleFor(rol => rol.NameRol)
            .Length(3, 70).WithMessage("El nombre de rol debe tener entre 3 y 70 caracteres.")
            .When(rol => rol.NameRol.Length > 0);

        RuleFor(rol => rol.Description)
            .Length(3, 70).WithMessage("La descripción del rol debe tener entre 3 y 70 caracteres.")
            .When(rol => rol.Description.Length > 0);
    }
}