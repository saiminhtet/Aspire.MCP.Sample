using McpSample.PostgreSQLMCPServer.Helper;
using ModelContextProtocol.Server;
using Npgsql;
using System.ComponentModel;

namespace McpSample.PostgreSQLMCPServer
{
    public partial class Tools
    {
        [McpServerTool(
            Title = "Read Data",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Executes SQL queries against PostgreSQL Database to read data")]
        public async Task<DbOperationResult> ReadData(
            [Description("SQL query to execute")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new NpgsqlCommand(sql, conn);
                    using var reader = await cmd.ExecuteReaderAsync();
                    var results = new List<Dictionary<string, object?>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                            // Auto-detect and decrypt encrypted string values
                            if (value is string stringValue && AESGCMEncryption.IsEncrypted(stringValue))
                            {
                                try
                                {
                                    value = AESGCMEncryption.TryDecrypt(stringValue);
                                    _logger.LogDebug("Successfully decrypted column: {ColumnName}", reader.GetName(i));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to decrypt column {ColumnName}, returning original value", reader.GetName(i));
                                    // Keep original value on decryption failure
                                }
                            }

                            row[reader.GetName(i)] = value;
                        }
                        results.Add(row);
                    }
                    return new DbOperationResult(success: true, data: results);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadData failed: {Message}", ex.Message);
                return new DbOperationResult(success: false, error: ex.Message);
            }
        }
    }
}
