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
            .Cascade(CascadeMode.Stop)
            .Length(3, 70).WithMessage("El nombre de rol debe tener entre 3 y 70 caracteres")
            .When(rol => rol.Description.Length== 0)
            .WithMessage("El nombre de rol es un dato obligatorio y minimamente debe tener 3 y maximo 70 caracteres.");

        RuleFor(rol => rol.Description)
            .Cascade(CascadeMode.Stop)
            .Length(3, 70).WithMessage("La descripción del rol debe tener entre 3 y 70 caracteres")
            .When(rol => rol.NameRol.Length== 0)
            .WithMessage("La descripción del rol es un dato obligatorio y minimamente debe tener 3 y maximo 70 caracteres.");


    }
}