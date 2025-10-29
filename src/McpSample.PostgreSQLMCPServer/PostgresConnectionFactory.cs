using Npgsql;

namespace McpSample.PostgreSQLMCPServer
{
    public class PostgresConnectionFactory: IPostgresConnectionFactory
    {
        private readonly string _connectionString;

        public PostgresConnectionFactory(IConfiguration configuration)
        {
            // Prefer "ConnectionStrings:PostgreSQL" section from appsettings.json
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException(
                    "Connection string 'PostgreSQL' not found in appsettings.json or environment variables.");
        }

        public async Task<NpgsqlConnection> GetOpenConnectionAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }
    }
}
