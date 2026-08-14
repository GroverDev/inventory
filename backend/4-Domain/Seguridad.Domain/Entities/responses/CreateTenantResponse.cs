namespace Seguridad.Domain.Entities.responses;

/// <summary>Farmacia recién dada de alta.</summary>
public class CreateTenantResponse
{
    public int TenantId { get; set; }

    public string Name { get; set; } = "";

    public string Slug { get; set; } = "";

    /// <summary>Correo del administrador inicial, que es también su usuario.</summary>
    public string AdminEmail { get; set; } = "";
}
