using ModelContextProtocol.Server;
using System.Data;

namespace McpSample.PostgreSQLMCPServer
{
    // Register this class as a tool container
    [McpServerToolType]
    public partial class Tools(IPostgresConnectionFactory connectionFactory, ILogger<Tools> logger)
    {
        private readonly IPostgresConnectionFactory _connectionFactory = connectionFactory;
        private readonly ILogger<Tools> _logger = logger;

        // Helper to convert DataTable to a serializable list
        private static List<Dictionary<string, object>> DataTableToList(DataTable table)
        {
            var result = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                result.Add(dict);
            }
            return result;
        }
    }
}
