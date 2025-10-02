﻿using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpSample.AspNetCoreSseServer
{
    public partial class Tools
    {
        [McpServerTool(
            Title = "Read Data",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Executes SQL queries against SQL Database to read data")]
        public async Task<DbOperationResult> ReadData(
            [Description("SQL query to execute")] string sql)
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    using var cmd = new SqlCommand(sql, conn);
                    using var reader = await cmd.ExecuteReaderAsync();
                    var results = new List<Dictionary<string, object?>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
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
