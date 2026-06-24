using System;
using System.IO;
using System.Threading;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ProjectChecking.Api.Data;

public class SchemaInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SchemaInitializer> _logger;
    private readonly IWebHostEnvironment _environment;

    public SchemaInitializer(IConfiguration configuration, IWebHostEnvironment environment, ILogger<SchemaInitializer> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public void Initialize()
    {
        var connectionString = _configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
        var builder = new SqlConnectionStringBuilder(connectionString);
        var targetDatabase = builder.InitialCatalog;
        if (string.IsNullOrEmpty(targetDatabase))
        {
            throw new InvalidOperationException("Initial Catalog (database name) must be set in the connection string.");
        }

        WaitForServer(builder);

        EnsureDatabaseExists(builder, targetDatabase);

        var scriptPath = ResolveScriptPath();
        if (!File.Exists(scriptPath))
        {
            _logger.LogWarning("SQL init script not found at {Path}; skipping initialization.", scriptPath);
            return;
        }

        var script = File.ReadAllText(scriptPath);

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        ExecuteBatched(connection, script);
        _logger.LogInformation("Database schema initialized successfully.");
    }

    private void WaitForServer(SqlConnectionStringBuilder builder)
    {
        var serverBuilder = new SqlConnectionStringBuilder(builder.ConnectionString) { InitialCatalog = "master" };
        var attempts = 30;
        for (var i = 1; i <= attempts; i++)
        {
            try
            {
                using var connection = new SqlConnection(serverBuilder.ConnectionString);
                connection.Open();
                return;
            }
            catch (Exception ex) when (i < attempts)
            {
                _logger.LogInformation("Waiting for SQL Server (attempt {Attempt}/{Total}): {Message}", i, attempts, ex.Message);
                Thread.Sleep(2000);
            }
        }
    }

    private void EnsureDatabaseExists(SqlConnectionStringBuilder builder, string databaseName)
    {
        var serverBuilder = new SqlConnectionStringBuilder(builder.ConnectionString) { InitialCatalog = "master" };
        using var connection = new SqlConnection(serverBuilder.ConnectionString);
        connection.Open();
        var exists = connection.ExecuteScalar<int?>("SELECT 1 FROM sys.databases WHERE name = @name", new { name = databaseName });
        if (exists != 1)
        {
            _logger.LogInformation("Creating database {Database}.", databaseName);
            var safeName = databaseName.Replace("]", "]]");
            connection.Execute($"CREATE DATABASE [{safeName}]");
        }
    }

    private string ResolveScriptPath()
    {
        var candidates = new[]
        {
            Path.Combine(_environment.ContentRootPath, "db", "init.sql"),
            Path.Combine(_environment.ContentRootPath, "..", "..", "db", "init.sql"),
            "/app/db/init.sql",
        };
        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (File.Exists(full))
            {
                return full;
            }
        }
        return candidates[0];
    }

    private static void ExecuteBatched(SqlConnection connection, string script)
    {
        var batches = SplitBatches(script);
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }
            connection.Execute(batch);
        }
    }

    private static System.Collections.Generic.IEnumerable<string> SplitBatches(string script)
    {
        var lines = script.Replace("\r\n", "\n").Split('\n');
        var current = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return current.ToString();
                current.Clear();
                continue;
            }
            current.AppendLine(line);
        }
        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }
}
