using System.Collections.Concurrent;
using Dapper;

namespace Inventory.Infrastructure;

/// <summary>
/// Nombre de la carpeta de medios de un tenant.
/// </summary>
/// <remarks>
/// Es su <c>public_id</c> (un UUID) y no el id numérico: el número es secuencial
/// y en una URL pública diría cuántas empresas hay y cuál es cada una.
/// </remarks>
public interface ITenantFolderResolver
{
    string FolderOf(int tenantId);
}

/// <summary>
/// Lo lee de <c>sec.tenants</c> y lo recuerda: es inmutable (lo garantiza un
/// trigger), así que el caché nunca queda viejo y cuesta una consulta por empresa
/// y por arranque del proceso.
/// </summary>
public sealed class TenantFolderResolver(InventoryDbContext _db) : ITenantFolderResolver
{
    private static readonly ConcurrentDictionary<int, string> Cache = new();

    public string FolderOf(int tenantId)
    {
        if (Cache.TryGetValue(tenantId, out var folder)) return folder;

        using var db = _db.CreateConnection;
        var publicId = db.QuerySingleOrDefault<Guid?>(
            "SELECT public_id FROM sec.tenants WHERE id = @tenantId", new { tenantId })
            ?? throw new InvalidOperationException($"No existe el tenant {tenantId}.");

        return Cache[tenantId] = publicId.ToString("D");
    }
}
