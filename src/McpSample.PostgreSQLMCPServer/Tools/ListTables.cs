using Npgsql;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpSample.PostgreSQLMCPServer
{
    public partial class Tools
    {
        private const string ListTablesQuery = @"
            SELECT schemaname, tablename
            FROM pg_catalog.pg_tables
            WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
            ORDER BY schemaname, tablename";

        [McpServerTool(
            Title = "List Tables",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Lists all tables in the PostgreSQL Database organized by schema.")]
        public async Task<DbOperationResult> ListTables()
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new NpgsqlCommand(ListTablesQuery, conn);
                    var tablesBySchema = new Dictionary<string, List<string>>();

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var schema = reader.GetString(0);
                        var tableName = reader.GetString(1);

                        if (!tablesBySchema.ContainsKey(schema))
                        {
                            tablesBySchema[schema] = new List<string>();
                        }

                        tablesBySchema[schema].Add($"{schema}.{tableName}");
                    }

                    // Format the output as a structured response
                    var formattedOutput = new
                    {
                        schemas = tablesBySchema.Select(kvp => new
                        {
                            schemaName = kvp.Key,
                            tables = kvp.Value
                        }).ToList(),
                        totalTables = tablesBySchema.Values.Sum(t => t.Count)
                    };

                    return new DbOperationResult(success: true, data: formattedOutput);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListTables failed: {Message}", ex.Message);
                return new DbOperationResult(success: false, error: ex.Message);
            }
        }
    }
}
