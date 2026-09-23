namespace Seguridad.Domain;

/// <summary>Sucursales en las que queda habilitado un usuario. Reemplaza las anteriores.</summary>
public class UserBranchesRequest
{
    public List<Guid> BranchIds { get; set; } = [];

    /// <summary>Sucursal a la que entra al iniciar sesión. Debe estar en <see cref="BranchIds"/>.</summary>
    public Guid DefaultBranchId { get; set; }
}
