using FluentValidation;

namespace Inventory.Domain.Entities.Requests;

public class CustomerRequest
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string Cellphone { get; set; } = "";
    public bool IsActive { get; set; }
}

/// <summary>
/// Antes de esto no había validación de servidor para el alta/edición de
/// clientes; solo la de Vuelidate en <c>CustomerEditView.vue</c>. Pasa a hacer
/// falta con el alta rápida desde el POS (web y móvil), que le pega al mismo
/// endpoint sin ese resguardo del lado cliente.
/// </summary>
public class CustomerRequestValidator : AbstractValidator<CustomerRequest>
{
    public CustomerRequestValidator()
    {
        RuleFor(p => p.FullName)
            .NotEmpty().WithMessage("El nombre del cliente es requerido.")
            .MaximumLength(150).WithMessage("El nombre no puede superar los {MaxLength} caracteres.");

        RuleFor(p => p.DocumentNumber)
            .NotEmpty().WithMessage("El número de documento es requerido.")
            .MaximumLength(20).WithMessage("El número de documento no puede superar los {MaxLength} caracteres.");

        RuleFor(p => p.Email)
            .MaximumLength(150).WithMessage("El correo no puede superar los {MaxLength} caracteres.");

        RuleFor(p => p.Cellphone)
            .MaximumLength(15).WithMessage("El celular no puede superar los {MaxLength} caracteres.");
    }
}
