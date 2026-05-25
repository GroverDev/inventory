using FluentValidation;
namespace Inventory.Domain;

public class ProductRequest
{
    public string Id { get; set; } = "";
    public string ProductCode { get; set; } ="";
    public string ProductName { get; set; }="";
    public string Description { get; set; }="";
    public decimal SalePrice { get; set; } = 0;
    public string UomId { get; set; }="";
    public int CurrentStock { get; set; }
    public bool IsActive { get; set; }
    public int MinReorderQuantity { get; set; }
    public bool AvailableInPos { get; set; }
    public string LaboratoryId { get; set; }="";
    public string CategoryId { get; set; }="";
    public string BarCode { get; set; } = "";
}


public class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(p => p.ProductName)
             .NotEmpty().WithMessage("El valor del nombre de producto es requerido.")
             .MinimumLength(5).WithMessage("El nombre de producto no puede ser menor a {MinLength} caracteres. ")
             .MaximumLength(50).WithMessage("El nombre de producto no puede ser mayor a {MaximumLength} caracteres. ");

        RuleFor(p => p.Description)
             .NotEmpty().WithMessage("El valor de la Descripción del producto es requerido.")
             .MinimumLength(5).WithMessage("La Descripción no puede ser menor a {MinLength} caracteres. ")
             .MaximumLength(500).WithMessage("La Descripción no puede ser mayor a {MaximumLength} caracteres. ");

    }
}