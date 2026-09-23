using Inventory.Application;

namespace Services.Api.Utils;

/// <summary>
/// De qué sucursales lee un reporte, según el parámetro <c>branch</c>:
/// <list type="bullet">
/// <item>vacío: la sucursal activa (lo que hace cualquier listado operativo);</item>
/// <item>un id: esa sucursal;</item>
/// <item><c>all</c>: el consolidado de todas.</item>
/// </list>
/// </summary>
/// <remarks>
/// Siempre acotado a las sucursales en las que el usuario está habilitado: el
/// selector de sucursal de un reporte no puede ser la puerta para ver una a la
/// que no tiene acceso operando. "Todas" significa todas las suyas.
/// </remarks>
public static class BranchScope
{
    public const string All = "all";

    /// <returns>
    /// Las sucursales a leer (null = la activa), o un mensaje si pidió una en
    /// la que no está habilitado.
    /// </returns>
    public static async Task<(Guid[]? Branches, string? Error)> Resolve(
        string? branch, int userId, IBranchApplication branches)
    {
        if (string.IsNullOrWhiteSpace(branch)) return (null, null);

        var habilitadas = await branches.GetUserBranchIds(userId);

        if (branch.Equals(All, StringComparison.OrdinalIgnoreCase))
            return (habilitadas.ToArray(), null);

        if (Guid.TryParse(branch, out Guid id) && habilitadas.Contains(id))
            return ([id], null);

        return (null, "No está habilitado en esa sucursal.");
    }
}
