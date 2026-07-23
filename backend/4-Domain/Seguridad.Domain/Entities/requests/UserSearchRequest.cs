using FluentValidation;

namespace Seguridad.Domain.Requests;

public class UserSearchRequest
{
    //public string UserName { get; set; }="";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    // Filtro de estado: true = solo activos (por defecto), false = solo inactivos, null = todos.
    public bool? IsActive { get; set; } = true;
}
public class UserSearchRequestValidator : AbstractValidator<UserSearchRequest>
{
    public UserSearchRequestValidator()
    {
        // Los filtros son opcionales: si ambos van vacíos se listan todos los usuarios.
        // Solo se valida la longitud cuando el campo trae un valor.
        RuleFor(user => user.Email)
            .Length(3, 70).WithMessage("El correo electrónico debe tener entre 3 y 70 caracteres")
            .When(user => !string.IsNullOrWhiteSpace(user.Email));

        RuleFor(user => user.FullName)
            .Length(3, 70).WithMessage("El nombre del usuario debe tener entre 3 y 70 caracteres")
            .When(user => !string.IsNullOrWhiteSpace(user.FullName));
    }
}