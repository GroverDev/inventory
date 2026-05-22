using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Seguridad.Infrastructure;

public class SeguridadDbContext
{
private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public SeguridadDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("SeguridadConnection")!;
    }

    public IDbConnection CreateConnection => new NpgsqlConnection(_connectionString);
}

