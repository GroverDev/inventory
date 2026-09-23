using System.Data;
using Common.Utilities.Exceptions;
using Dapper;
using Seguridad.Domain;

namespace Seguridad.Infrastructure;

/// <summary>
/// Elige la sucursal con la que se emite un JWT. La usan todos los caminos que
/// completan una sesión: login, verificación TOTP, dispositivo de confianza y
/// refresh.
/// </summary>
internal static class LoginBranches
{
    /// <summary>
    /// Completa <see cref="LoginResponse.Branches"/> y fija la sucursal activa:
    /// <paramref name="preferred"/> si el usuario sigue habilitado en ella, si no
    /// su sucursal default.
    /// </summary>
    /// <remarks>
    /// Corre sobre <c>sec.fn_auth_branches</c> porque estos caminos no tienen
    /// tenant en la sesión, y <c>sec.users_branches</c> está bajo RLS.
    /// <para>
    /// Volver a consultar en cada refresh es lo que hace efectivo quitarle una
    /// sucursal a un usuario: su sesión en ella dura, a lo sumo, lo que le quede
    /// al access token.
    /// </para>
    /// </remarks>
    /// <exception cref="CustomException">El usuario no está habilitado en ninguna sucursal activa.</exception>
    public static async Task Assign(IDbConnection db, LoginResponse usuario, Guid? preferred = null,
                                    IDbTransaction? transaction = null)
    {
        var branches = (await db.QueryAsync<BranchOption>(
            "SELECT branch_id, name, is_default FROM sec.fn_auth_branches(@user_id)",
            new { user_id = usuario.UserId }, transaction)).ToList();

        if (branches.Count == 0)
            throw new CustomException(
                "Tu usuario no está habilitado en ninguna sucursal activa. Contacta al administrador.");

        // La función devuelve la default primero; si nadie la marcó (se la
        // quitaron y no se eligió otra), la primera por nombre es tan buena
        // como cualquiera y evita dejar al usuario afuera.
        var activa = branches.FirstOrDefault(b => preferred.HasValue && b.BranchId == preferred.Value)
                     ?? branches[0];

        usuario.Branches = branches;
        usuario.BranchId = activa.BranchId;
        usuario.BranchName = activa.Name;
    }
}
