using FluentValidation;

namespace Inventory.Domain;

/// <summary>Venta que el cajero deja en espera para retomarla después.</summary>
public class HeldSaleRequest
{
    /// <summary>Para reconocerla en la lista: "señor de camisa azul", "mesa 3".</summary>
    public string Label { get; set; } = "";
    public Guid? CustomerId { get; set; }
    public int ItemsCount { get; set; }
    public decimal Total { get; set; }

    /// <summary>
    /// El carrito como lo arma el punto de venta, en JSON. El servidor no lo
    /// interpreta: al cobrar, la venta se valida entera como cualquier otra.
    /// </summary>
    public string Payload { get; set; } = "";
}

public class HeldSaleRequestValidator : AbstractValidator<HeldSaleRequest>
{
    /// <summary>Un carrito real ocupa unos pocos KB; esto solo frena abusos.</summary>
    public const int MaxPayloadChars = 200_000;

    public HeldSaleRequestValidator()
    {
        RuleFor(h => h.Label).MaximumLength(100).WithMessage("La nota no puede superar {MaxLength} caracteres.");
        RuleFor(h => h.ItemsCount).GreaterThan(0).WithMessage("No se puede poner en espera un carrito vacío.");
        RuleFor(h => h.Payload)
            .NotEmpty().WithMessage("Falta el contenido del carrito.")
            .MaximumLength(MaxPayloadChars).WithMessage("El carrito es demasiado grande para ponerlo en espera.");
    }
}

/// <summary>Venta en espera, como se lista en el punto de venta.</summary>
public class HeldSaleResponse
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateTime Created { get; set; }
    public int ItemsCount { get; set; }
    public decimal Total { get; set; }

    /// <summary>Solo al retomarla: el carrito guardado.</summary>
    public string Payload { get; set; } = "";
}
