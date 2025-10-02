using Microsoft.Data.SqlClient;

namespace McpSample.AspNetCoreSseServer
{
    public class SqlConnectionFactory: ISqlConnectionFactory
    {
        //public async Task<SqlConnection> GetOpenConnectionAsync()
        //{
        //    var connectionString = GetConnectionString();

        //    // Let ADO.Net handle connection pooling
        //    var conn = new SqlConnection(connectionString);
        //    await conn.OpenAsync();
        //    return conn;
        //}

        //private static string GetConnectionString()
        //{
        //    var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

        //    return string.IsNullOrEmpty(connectionString)
        //        ? throw new InvalidOperationException("Connection string is not set in the environment variable 'CONNECTION_STRING'.\n\nHINT: Have a local SQL Server, with a database called 'test', from console, run `SET CONNECTION_STRING=Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True` and the load the .sln file")
        //        : connectionString;
        //}

        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            // Prefer "ConnectionStrings:MSSQL" section from appsettings.json
            _connectionString = configuration.GetConnectionString("MSSQL")
                ?? throw new InvalidOperationException(
                    "Connection string 'MSSQL' not found in appsettings.json or environment variables.");
        }

        public async Task<SqlConnection> GetOpenConnectionAsync()
        {
            var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }
    }
}
