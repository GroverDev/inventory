using Seguridad.Domain.Entities.requests;
using Seguridad.Domain.Entities.responses;

namespace Seguridad.Infrastructure;

public interface IAdminRepository
{
    /// <summary>
    /// Indica si el usuario tiene asignado (activo) el rol indicado.
    /// </summary>
    Task<bool> UserHasActiveRole(int userId, string roleName);

    /// <summary>
    /// Indica si el usuario puede ejecutar operaciones de plataforma, como dar de
    /// alta farmacias. Es un atributo aparte del sistema de roles: cada farmacia
    /// tiene su propio SuperAdmin, así que ese rol no sirve para autorizarlas.
    /// </summary>
    Task<bool> UserIsPlatformAdmin(int userId);

    /// <summary>
    /// Da de alta una farmacia con sus datos maestros mínimos, su rol SuperAdmin y
    /// su usuario administrador. Todo en una transacción.
    /// </summary>
    Task<CreateTenantResponse> CreateTenant(CreateTenantRequest request);

    /// <summary>
    /// Reinicia por completo la base de datos dejándola lista para una empresa nueva:
    /// respalda, trunca todas las tablas de datos, reinicia contadores y crea el nuevo administrador.
    /// Todo se ejecuta en una única transacción. Devuelve el nombre del esquema de respaldo generado
    /// (cadena vacía si se omitió el respaldo).
    /// </summary>
    Task<string> ResetCompany(ResetCompanyRequest request, int executedByUserId);
}
