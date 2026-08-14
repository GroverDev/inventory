using System.Data;
using Common.Utilities.MultiTenancy;
using Microsoft.Extensions.Configuration;

namespace Seguridad.Infrastructure;

public class SeguridadDbContext
{
    private readonly string _connectionString;
    private readonly ITenantContext _tenant;

    public SeguridadDbContext(IConfiguration configuration, ITenantContext tenant)
    {
        _tenant = tenant;
        _connectionString = ConnectionStringResolver.Resolve(configuration);
    }

    /// <summary>
    /// Conexión para operaciones ya autenticadas (usuarios, roles, MFA). Exige
    /// tenant resuelto.
    /// </summary>
    public IDbConnection CreateConnection =>
        TenantConnectionFactory.Create(_connectionString, _tenant, requiereTenant: true);

    /// <summary>
    /// Conexión para el camino de autenticación, <b>antes</b> de saber quién entra.
    /// </summary>
    /// <remarks>
    /// El login busca al usuario por correo sin tenant, que es la única consulta
    /// del sistema que legítimamente no puede tenerlo. Usarla en cualquier otro
    /// lugar abre un agujero de aislamiento: con RLS activo, estas conexiones solo
    /// pueden llegar a lo que la política de autenticación permita explícitamente.
    /// </remarks>
    public IDbConnection CreateAuthConnection =>
        TenantConnectionFactory.Create(_connectionString, _tenant, requiereTenant: false);
}
