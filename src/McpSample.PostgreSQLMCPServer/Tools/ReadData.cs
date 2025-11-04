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
                    var decryptionErrors = new List<string>();

                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                            // Auto-detect and decrypt encrypted string values
                            if (value is string stringValue)
                            {
                                var (success, decryptedValue, error) = AESGCMEncryption.TryDecryptWithError(stringValue);

                                if (!success && error != null)
                                {
                                    var errorMsg = $"Column '{reader.GetName(i)}': {error}";
                                    decryptionErrors.Add(errorMsg);
                                    _logger.LogWarning("Decryption failed for column {ColumnName}: {Error}", reader.GetName(i), error);
                                }

                                value = decryptedValue;
                            }

                            row[reader.GetName(i)] = value;
                        }
                        results.Add(row);
                    }

                    // Log summary of decryption errors if any occurred
                    if (decryptionErrors.Count > 0)
                    {
                        _logger.LogWarning("Total decryption errors: {Count}", decryptionErrors.Count);
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
