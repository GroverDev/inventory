using FluentValidation;

namespace Seguridad.Domain.Entities.requests;

public class ChangeOwnPasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class ChangeOwnPasswordRequestValidator : AbstractValidator<ChangeOwnPasswordRequest>
{
    public ChangeOwnPasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es requerida.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es requerida.")
            .MinimumLength(6).WithMessage("La nueva contraseña debe tener al menos 6 caracteres.");
    }
}
