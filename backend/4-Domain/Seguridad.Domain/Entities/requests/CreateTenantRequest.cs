using FluentValidation;

namespace Seguridad.Domain.Entities.requests;

/// <summary>
/// Alta de una farmacia nueva con su administrador inicial.
/// </summary>
/// <remarks>
/// Operación de plataforma, no de farmacia: solo la puede ejecutar un usuario con
/// <c>is_platform_admin</c>. El rol SuperAdmin no alcanza, porque cada farmacia
/// tiene el suyo.
/// </remarks>
public class CreateTenantRequest
{
    /// <summary>Nombre comercial de la farmacia.</summary>
    public string Name { get; set; } = "";

    /// <summary>Identificador corto y estable, usable en URL o subdominio.</summary>
    public string Slug { get; set; } = "";

    /// <summary>Correo del administrador inicial. Es también su nombre de usuario.</summary>
    public string AdminEmail { get; set; } = "";

    /// <summary>Nombre completo del administrador inicial.</summary>
    public string AdminFullName { get; set; } = "";

    /// <summary>
    /// Contraseña inicial del administrador. Se le exigirá cambiarla al primer ingreso.
    /// </summary>
    public string AdminPassword { get; set; } = "";
}

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la farmacia es requerido.")
            .MaximumLength(150).WithMessage("El nombre no puede superar los {MaxLength} caracteres.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("El identificador es requerido.")
            .MaximumLength(60).WithMessage("El identificador no puede superar los {MaxLength} caracteres.")
            // Se restringe a minúsculas, números y guiones porque está pensado para
            // usarse en una URL o un subdominio.
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("El identificador solo admite minúsculas, números y guiones, sin espacios (ejemplo: san-jose).");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("El correo del administrador es requerido.")
            .EmailAddress().WithMessage("Formato de correo electrónico incorrecto.")
            .MaximumLength(50).WithMessage("El correo no puede superar los {MaxLength} caracteres.");

        RuleFor(x => x.AdminFullName)
            .NotEmpty().WithMessage("El nombre del administrador es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los {MaxLength} caracteres.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("La contraseña del administrador es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos {MinLength} caracteres.")
            .MaximumLength(50).WithMessage("La contraseña no puede superar los {MaxLength} caracteres.");
    }
}
