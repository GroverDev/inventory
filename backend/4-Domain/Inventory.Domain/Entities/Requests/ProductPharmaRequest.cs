using FluentValidation;

namespace Inventory.Domain;

/// <summary>Un componente del producto tal como llega del cliente.</summary>
public class ProductComponentRequest
{
    /// <summary>
    /// Nulable a propósito: ASP.NET trata un string NO nulable como obligatorio,
    /// y este campo es justamente el que puede faltar cuando la sustancia se da
    /// de alta por nombre.
    /// </summary>
    public string? SubstanceId { get; set; }

    /// <summary>
    /// Alternativa a <see cref="SubstanceId"/>: nombre de una sustancia que
    /// todavía no está en el catálogo. Se da de alta al vuelo.
    /// </summary>
    /// <remarks>
    /// Sin esto, cargar un producto obligaría a salir a otra pantalla a crear la
    /// sustancia y volver. Con 1.200 productos por cargar, esa fricción es la
    /// diferencia entre que el catálogo se llene y que quede vacío.
    /// </remarks>
    public string? SubstanceName { get; set; }

    public decimal? ConcentrationValue { get; set; }
    public string? ConcentrationUnit { get; set; }
    public bool IsActiveIngredient { get; set; } = true;
}

/// <summary>Datos farmacéuticos de un producto, para guardar en un solo viaje.</summary>
public class ProductPharmaRequest
{
    public string? FormId { get; set; }
    public string? RouteId { get; set; }
    public string? Presentation { get; set; }
    public string? DosageReference { get; set; }
    public string? ProductType { get; set; }
    public string? SanitaryRegistry { get; set; }

    /// <summary>ISO (yyyy-MM-dd), como el resto de las fechas del sistema.</summary>
    public string? SanitaryRegistryExpiry { get; set; }

    public List<ProductComponentRequest> Components { get; set; } = [];
}

public class ProductPharmaRequestValidator : AbstractValidator<ProductPharmaRequest>
{
    private static readonly string[] TiposValidos = ["generico", "marca", "similar"];

    public ProductPharmaRequestValidator()
    {
        RuleFor(x => x.ProductType)
            .Must(t => string.IsNullOrWhiteSpace(t) || TiposValidos.Contains(t))
            .WithMessage("El tipo de producto debe ser genérico, marca o similar.");

        RuleFor(x => x.Presentation)
            .MaximumLength(150).WithMessage("La presentación no puede superar los {MaxLength} caracteres.");

        RuleFor(x => x.DosageReference)
            .MaximumLength(300).WithMessage("La posología no puede superar los {MaxLength} caracteres.");

        RuleFor(x => x.SanitaryRegistry)
            .MaximumLength(60).WithMessage("El registro sanitario no puede superar los {MaxLength} caracteres.");

        RuleForEach(x => x.Components).ChildRules(c =>
        {
            // Se acepta una de las dos formas: la sustancia ya existe, o viene
            // por nombre para darla de alta.
            c.RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.SubstanceId)
                        || !string.IsNullOrWhiteSpace(x.SubstanceName))
                .WithMessage("Cada componente necesita una sustancia.");

            c.RuleFor(x => x.ConcentrationValue)
                .GreaterThan(0).When(x => x.ConcentrationValue.HasValue)
                .WithMessage("La concentración debe ser mayor a cero.");
        });
    }
}
