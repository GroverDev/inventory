using FluentValidation;

namespace Seguridad.Domain.Entities.requests;

/// <summary>
/// Petición para reiniciar por completo la base de datos y dejarla lista para una empresa nueva.
/// Operación destructiva e irreversible: solo un usuario con rol SuperAdmin puede ejecutarla.
/// </summary>
public class ResetCompanyRequest
{
    /// <summary>Contraseña actual del super usuario que ejecuta la acción (re-autenticación).</summary>
    public string CurrentPassword { get; set; } = "";

    /// <summary>Frase de seguridad que el usuario debe escribir literalmente para confirmar.</summary>
    public string ConfirmationPhrase { get; set; } = "";

    /// <summary>Correo del nuevo administrador que quedará en la empresa nueva.</summary>
    public string NewAdminEmail { get; set; } = "";

    /// <summary>Nombre completo del nuevo administrador.</summary>
    public string NewAdminFullName { get; set; } = "";

    /// <summary>Contraseña del nuevo administrador.</summary>
    public string NewAdminPassword { get; set; } = "";

    /// <summary>Si es true, omite el respaldo previo (no recomendado).</summary>
    public bool SkipBackup { get; set; } = false;

    /// <summary>Frase literal esperada en <see cref="ConfirmationPhrase"/>.</summary>
    public const string ExpectedPhrase = "RESETEAR EMPRESA";
}

public class ResetCompanyRequestValidator : AbstractValidator<ResetCompanyRequest>
{
    public ResetCompanyRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es requerida.");

        RuleFor(x => x.ConfirmationPhrase)
            .NotEmpty().WithMessage("La frase de confirmación es requerida.")
            .Must(p => p != null && p.Trim() == ResetCompanyRequest.ExpectedPhrase)
            .WithMessage($"Debe escribir exactamente «{ResetCompanyRequest.ExpectedPhrase}» para confirmar.");

        RuleFor(x => x.NewAdminEmail)
            .NotEmpty().WithMessage("El correo del nuevo administrador es requerido.")
            .EmailAddress().WithMessage("Formato de correo electrónico incorrecto.")
            .MaximumLength(50).WithMessage("El correo no puede superar los {MaxLength} caracteres.");

        RuleFor(x => x.NewAdminFullName)
            .NotEmpty().WithMessage("El nombre del nuevo administrador es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los {MaxLength} caracteres.");

        RuleFor(x => x.NewAdminPassword)
            .NotEmpty().WithMessage("La contraseña del nuevo administrador es requerida.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos {MinLength} caracteres.")
            .MaximumLength(50).WithMessage("La contraseña no puede superar los {MaxLength} caracteres.");
    }
}
