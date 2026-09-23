using FluentValidation;

namespace Inventory.Domain;

public class BranchRequest
{
    public string Id { get; set; } = Guid.Empty.ToString();
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public bool IsActive { get; set; }

    /// <inheritdoc cref="Branch.UsersCount"/>
    public int UsersCount { get; set; }
}

public class BranchRequestValidator : AbstractValidator<BranchRequest>
{
    public BranchRequestValidator()
    {
        RuleFor(b => b.Name)
            .NotEmpty().WithMessage("El nombre de la sucursal es requerido.")
            .MinimumLength(2).WithMessage("El nombre no puede ser menor a {MinLength} caracteres.")
            .MaximumLength(150).WithMessage("El nombre no puede ser mayor a {MaxLength} caracteres.");
        RuleFor(b => b.Code)
            .MaximumLength(20).WithMessage("El código no puede ser mayor a {MaxLength} caracteres.");
        RuleFor(b => b.Address)
            .MaximumLength(300).WithMessage("La dirección no puede ser mayor a {MaxLength} caracteres.");
        RuleFor(b => b.Phone)
            .MaximumLength(50).WithMessage("El teléfono no puede ser mayor a {MaxLength} caracteres.");
    }
}
