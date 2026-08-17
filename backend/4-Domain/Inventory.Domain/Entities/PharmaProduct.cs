using Common.Utilities;

namespace Inventory.Domain;

/// <summary>
/// Sustancia del catálogo: principio activo o excipiente.
/// </summary>
/// <remarks>
/// Ambos van al mismo catálogo porque son sustancias; lo que cambia es el papel
/// que cumplen en cada producto, y eso se marca en <see cref="ProductComponent"/>.
/// </remarks>
public class PharmaSubstance : Audit
{
    public Guid Id { get; set; }
    public string SubstanceName { get; set; } = "";

    /// <summary>
    /// Analgésicos, antibióticos, antihistamínicos. Habilita sugerir productos
    /// de la misma acción cuando no hay equivalente exacto. Es sugerencia
    /// comercial, no equivalencia clínica.
    /// </summary>
    public string? TherapeuticGroup { get; set; }
}

/// <summary>
/// Un componente del producto, con su concentración.
/// </summary>
/// <remarks>
/// La concentración vive acá y no en la sustancia: dos cremas con ácido
/// salicílico al 2% y al 5% comparten sustancia y difieren en proporción. Un
/// producto puede tener varios — los antigripales son casi siempre combinaciones.
/// </remarks>
public class ProductComponent : Audit
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid SubstanceId { get; set; }

    /// <summary>Solo para mostrar; el dato vive en el catálogo.</summary>
    public string SubstanceName { get; set; } = "";

    public decimal? ConcentrationValue { get; set; }
    public string? ConcentrationUnit { get; set; }

    /// <summary>
    /// <c>false</c> para los excipientes (lactosa, gluten, azúcar). No sirven
    /// para buscar equivalentes, pero sí importan clínicamente.
    /// </summary>
    public bool IsActiveIngredient { get; set; } = true;

    public int ShowOrder { get; set; }
}

/// <summary>Datos farmacéuticos del producto. Uno a uno, y solo si aplica.</summary>
public class ProductPharma : Audit
{
    public Guid ProductId { get; set; }
    public Guid? FormId { get; set; }
    public Guid? RouteId { get; set; }

    /// <summary>Solo para mostrar.</summary>
    public string FormName { get; set; } = "";
    public string RouteName { get; set; } = "";

    /// <summary>"caja x 20 comprimidos", "frasco 120 ml".</summary>
    public string? Presentation { get; set; }

    /// <summary>
    /// Posología del prospecto, para consulta del mostrador. NO es una
    /// recomendación del sistema: la dosis real depende del paciente.
    /// </summary>
    public string? DosageReference { get; set; }

    /// <summary>'generico' | 'marca' | 'similar'.</summary>
    public string? ProductType { get; set; }

    public string? SanitaryRegistry { get; set; }
    public DateTime? SanitaryRegistryExpiry { get; set; }

    public List<ProductComponent> Components { get; set; } = [];
}

/// <summary>Catálogo corto: forma farmacéutica o vía de administración.</summary>
public class PharmaCatalogItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}
