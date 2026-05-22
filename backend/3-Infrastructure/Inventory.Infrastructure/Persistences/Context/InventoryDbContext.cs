using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Inventory.Infrastructure;

public class InventoryDbContext
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public InventoryDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("InventoryConnection")!;
    }

    public IDbConnection CreateConnection => new NpgsqlConnection(_connectionString);
}
