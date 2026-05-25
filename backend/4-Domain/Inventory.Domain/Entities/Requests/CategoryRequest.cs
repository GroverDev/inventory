using FluentValidation;

namespace Inventory.Domain;

public class CategoryRequest
{
    public string Id { get; set; } = Guid.Empty.ToString();
    public string CategoryName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; }
}

public class CategoryRequestValidator : AbstractValidator<CategoryRequest>
{
    public CategoryRequestValidator()
    {
        RuleFor(c => c.CategoryName)
            .NotEmpty().WithMessage("El nombre de la categoría es requerido.")
            .MinimumLength(2).WithMessage("El nombre no puede ser menor a {MinLength} caracteres.")
            .MaximumLength(100).WithMessage("El nombre no puede ser mayor a {MaximumLength} caracteres.");
    }
}
