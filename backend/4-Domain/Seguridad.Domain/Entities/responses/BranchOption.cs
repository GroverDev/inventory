namespace Seguridad.Domain;

/// <summary>Sucursal en la que un usuario está habilitado, para el selector de sucursal.</summary>
public class BranchOption
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Sucursal a la que el usuario entra al iniciar sesión.</summary>
    public bool IsDefault { get; set; }
}
