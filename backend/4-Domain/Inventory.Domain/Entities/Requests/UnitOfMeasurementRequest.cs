using System;
using FluentValidation;

namespace Inventory.Domain.Entities.Requests;

public class UnitOfMeasurementRequest
{
    public string Id { get; set; } = Guid.Empty.ToString();
    public string Name { get; set; } = "";
    public int Proportion { get; set; }
    public int PrecisionRounding { get; set; }
    public bool IsLargeThanDefault { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class UnitOfMeasurementRequestValidator : AbstractValidator<UnitOfMeasurementRequest>
{
    public UnitOfMeasurementRequestValidator()
    {
        RuleFor(p => p.Name)
             .NotEmpty().WithMessage("El valor del nombre de la unidad de medida es requerido.")
             .MinimumLength(3).WithMessage("El nombre de la unidad de medida no puede ser menor a {MinLength} caracteres. ")
             .MaximumLength(50).WithMessage("El nombre de la unidad de medida no puede ser mayor a {MaximumLength} caracteres. ");
    }
}
