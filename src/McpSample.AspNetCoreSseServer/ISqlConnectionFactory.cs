using Microsoft.Data.SqlClient;

namespace McpSample.AspNetCoreSseServer
{
    public interface ISqlConnectionFactory
    {
        Task<SqlConnection> GetOpenConnectionAsync();
    }
}
