namespace Inventory.Domain;

/// <summary>Arqueo de un medio de pago en el cierre de caja.</summary>
public class CashCountResponse
{
    public Guid PaymentMethodId { get; set; }
    public string Name { get; set; } = "";
    public string IconCss { get; set; } = "";
    public decimal Expected { get; set; }
    public decimal Declared { get; set; }

    /// <summary>Declarado − esperado: positivo es sobrante, negativo faltante.</summary>
    public decimal Difference { get; set; }
}

/// <summary>Configuración del cierre de caja de la empresa.</summary>
public class CashCloseSettings
{
    /// <summary>Diferencia por medio a partir de la cual el cierre exige observación.</summary>
    public decimal NoteThreshold { get; set; } = 10;

    /// <summary>
    /// Diferencia por medio a partir de la cual, además de la observación, el
    /// cierre necesita un supervisor. Nulo = nunca.
    /// </summary>
    public decimal? SupervisorThreshold { get; set; }

    /// <summary>Cierres rechazados a partir de los cuales hace falta un supervisor. Nulo = sin límite.</summary>
    public int? MaxAttempts { get; set; }

    /// <summary>El efectivo se declara contando billetes y monedas.</summary>
    public bool RequireDenominations { get; set; }
    public List<PaymentMethod> Methods { get; set; } = [];
}

public class CashCloseSettingsRequest
{
    public decimal NoteThreshold { get; set; }
    public decimal? SupervisorThreshold { get; set; }
    public int? MaxAttempts { get; set; }
    public bool RequireDenominations { get; set; }

    /// <summary>Qué medios se arquean. El efectivo se arquea siempre, se mande lo que se mande.</summary>
    public List<CashCloseMethodRequest> Methods { get; set; } = [];
}

public class CashCloseMethodRequest
{
    public Guid Id { get; set; }
    public bool RequiresCount { get; set; }
}

/// <summary>Cantidad contada de un billete o moneda.</summary>
public class DenominationCount
{
    /// <summary>Valor del billete o moneda (200, 100, …, 0.10).</summary>
    public decimal Value { get; set; }
    public int Quantity { get; set; }
}
