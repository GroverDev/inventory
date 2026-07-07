using Seguridad.Domain.Entities.requests;

namespace Seguridad.Infrastructure;

public interface IAdminRepository
{
    /// <summary>
    /// Indica si el usuario tiene asignado (activo) el rol indicado.
    /// </summary>
    Task<bool> UserHasActiveRole(int userId, string roleName);

    /// <summary>
    /// Reinicia por completo la base de datos dejándola lista para una empresa nueva:
    /// respalda, trunca todas las tablas de datos, reinicia contadores y crea el nuevo administrador.
    /// Todo se ejecuta en una única transacción. Devuelve el nombre del esquema de respaldo generado
    /// (cadena vacía si se omitió el respaldo).
    /// </summary>
    Task<string> ResetCompany(ResetCompanyRequest request, int executedByUserId);
}
