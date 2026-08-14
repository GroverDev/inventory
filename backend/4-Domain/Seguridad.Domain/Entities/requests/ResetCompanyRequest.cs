using FluentValidation;

namespace Seguridad.Domain.Entities.requests;

/// <summary>
/// Petición para vaciar los datos de negocio de la farmacia que ejecuta la acción.
/// </summary>
/// <remarks>
/// Borra productos, ventas, compras, clientes, stock, caja y datos maestros, y deja
/// sembrados los mínimos para poder volver a operar. <b>Conserva usuarios, roles y
/// permisos</b>: quien reinicia no se borra a sí mismo.
/// <para>
/// Alcanza únicamente a la farmacia de la sesión. En la versión de un solo cliente
/// esta operación vaciaba la base entera y creaba un administrador nuevo; ese
/// comportamiento sería catastrófico con varias farmacias conviviendo.
/// </para>
/// Operación destructiva e irreversible: solo un usuario con rol SuperAdmin puede ejecutarla.
/// </remarks>
public class ResetCompanyRequest
{
    /// <summary>Contraseña actual del super usuario que ejecuta la acción (re-autenticación).</summary>
    public string CurrentPassword { get; set; } = "";

    /// <summary>Frase de seguridad que el usuario debe escribir literalmente para confirmar.</summary>
    public string ConfirmationPhrase { get; set; } = "";

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
    }
}
