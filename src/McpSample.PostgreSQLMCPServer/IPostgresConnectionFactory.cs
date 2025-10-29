using Npgsql;

namespace McpSample.PostgreSQLMCPServer
{
    public interface IPostgresConnectionFactory
    {
        Task<NpgsqlConnection> GetOpenConnectionAsync();
    }
}
