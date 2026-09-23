namespace Seguridad.Domain;

/// <summary>Una sucursal activa de la farmacia, y si el usuario está habilitado en ella.</summary>
public class UserBranchResponse
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public bool IsDefault { get; set; }
}
