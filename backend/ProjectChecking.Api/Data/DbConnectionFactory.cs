using System.Data;
using Microsoft.Data.SqlClient;

namespace ProjectChecking.Api.Data;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
    }

    public IDbConnection Create() => new SqlConnection(_connectionString);
}
