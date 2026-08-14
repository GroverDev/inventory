using System.Data;
using Common.Utilities.MultiTenancy;
using Microsoft.Extensions.Configuration;

namespace Inventory.Infrastructure;

public class InventoryDbContext
{
    private readonly string _connectionString;
    private readonly ITenantContext _tenant;

    public InventoryDbContext(IConfiguration configuration, ITenantContext tenant)
    {
        _tenant = tenant;
        _connectionString = ConnectionStringResolver.Resolve(configuration);
    }

    /// <summary>
    /// Conexión a los datos de negocio. Exige tenant resuelto: acá no hay ningún
    /// caso legítimo sin él.
    /// </summary>
    public IDbConnection CreateConnection =>
        TenantConnectionFactory.Create(_connectionString, _tenant, requiereTenant: true);
}
