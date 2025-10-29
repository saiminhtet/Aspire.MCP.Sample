using Npgsql;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpSample.PostgreSQLMCPServer
{
    public partial class Tools
    {
        [McpServerTool(
            Title = "Describe Table",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Returns table schema")]
        public async Task<DbOperationResult> DescribeTable(
            [Description("Name of table")] string name)
        {
            string? schema = "public";
            if (name.Contains('.'))
            {
                // If the table name contains a schema, split it into schema and table name
                var parts = name.Split('.');
                if (parts.Length > 1)
                {
                    schema = parts[0]; // Use the first part as schema
                    name = parts[1]; // Use only the table name part
                }
            }

            // Query for table metadata
            const string TableInfoQuery = @"
                SELECT
                    c.oid AS id,
                    c.relname AS name,
                    n.nspname AS schema,
                    obj_description(c.oid) AS description,
                    c.relkind AS type,
                    pg_get_userbyid(c.relowner) AS owner
                FROM pg_class c
                JOIN pg_namespace n ON c.relnamespace = n.oid
                WHERE c.relkind = 'r'
                  AND c.relname = @TableName
                  AND n.nspname = @TableSchema";

            // Query for columns
            const string ColumnsQuery = @"
                SELECT
                    a.attname AS name,
                    format_type(a.atttypid, a.atttypmod) AS type,
                    a.attlen AS length,
                    a.atttypmod AS precision,
                    0 AS scale,
                    NOT a.attnotnull AS nullable,
                    col_description(a.attrelid, a.attnum) AS description
                FROM pg_attribute a
                JOIN pg_class c ON a.attrelid = c.oid
                JOIN pg_namespace n ON c.relnamespace = n.oid
                WHERE c.relname = @TableName
                  AND n.nspname = @TableSchema
                  AND a.attnum > 0
                  AND NOT a.attisdropped
                ORDER BY a.attnum";

            // Query for indexes
            const string IndexesQuery = @"
                SELECT
                    i.relname AS name,
                    am.amname AS type,
                    obj_description(i.oid) AS description,
                    string_agg(a.attname, ',' ORDER BY array_position(ix.indkey, a.attnum)) AS keys
                FROM pg_index ix
                JOIN pg_class i ON ix.indexrelid = i.oid
                JOIN pg_class c ON ix.indrelid = c.oid
                JOIN pg_namespace n ON c.relnamespace = n.oid
                JOIN pg_am am ON i.relam = am.oid
                LEFT JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = ANY(ix.indkey)
                WHERE c.relname = @TableName
                  AND n.nspname = @TableSchema
                  AND NOT ix.indisprimary
                  AND NOT ix.indisunique
                GROUP BY i.relname, am.amname, i.oid
                ORDER BY i.relname";

            // Query for constraints
            const string ConstraintsQuery = @"
                SELECT
                    con.conname AS name,
                    con.contype AS type,
                    string_agg(a.attname, ',' ORDER BY array_position(con.conkey, a.attnum)) AS keys
                FROM pg_constraint con
                JOIN pg_class c ON con.conrelid = c.oid
                JOIN pg_namespace n ON c.relnamespace = n.oid
                LEFT JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = ANY(con.conkey)
                WHERE c.relname = @TableName
                  AND n.nspname = @TableSchema
                  AND con.contype IN ('p', 'u', 'c')
                GROUP BY con.conname, con.contype
                ORDER BY con.conname";

            // Query for foreign keys
            const string ForeignKeyInformation = @"
                SELECT
                    con.conname AS name,
                    n.nspname AS schema,
                    c.relname AS table_name,
                    string_agg(a.attname, ', ' ORDER BY array_position(con.conkey, a.attnum)) AS column_names,
                    fn.nspname AS referenced_schema,
                    fc.relname AS referenced_table,
                    string_agg(fa.attname, ', ' ORDER BY array_position(con.confkey, fa.attnum)) AS referenced_column_names
                FROM pg_constraint con
                JOIN pg_class c ON con.conrelid = c.oid
                JOIN pg_namespace n ON c.relnamespace = n.oid
                JOIN pg_class fc ON con.confrelid = fc.oid
                JOIN pg_namespace fn ON fc.relnamespace = fn.oid
                LEFT JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = ANY(con.conkey)
                LEFT JOIN pg_attribute fa ON fa.attrelid = fc.oid AND fa.attnum = ANY(con.confkey)
                WHERE c.relname = @TableName
                  AND n.nspname = @TableSchema
                  AND con.contype = 'f'
                GROUP BY con.conname, n.nspname, c.relname, fn.nspname, fc.relname
                ORDER BY con.conname";

            var conn = await _connectionFactory.GetOpenConnectionAsync();
            try
            {
                using (conn)
                {
                    var result = new Dictionary<string, object>();
                    // Table info
                    using (var cmd = new NpgsqlCommand(TableInfoQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TableName", name);
                        cmd.Parameters.AddWithValue("@TableSchema", schema);
                        using var reader = await cmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            result["table"] = new
                            {
                                id = reader["id"],
                                name = reader["name"],
                                schema = reader["schema"],
                                owner = reader["owner"],
                                type = reader["type"],
                                description = reader["description"] is DBNull ? null : reader["description"]
                            };
                        }
                        else
                        {
                            return new DbOperationResult(success: false, error: $"Table '{name}' not found in schema '{schema}'.");
                        }
                    }
                    // Columns
                    using (var cmd = new NpgsqlCommand(ColumnsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TableName", name);
                        cmd.Parameters.AddWithValue("@TableSchema", schema);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var columns = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            columns.Add(new
                            {
                                name = reader["name"],
                                type = reader["type"],
                                length = reader["length"],
                                precision = reader["precision"],
                                scale = reader["scale"],
                                nullable = (bool)reader["nullable"],
                                description = reader["description"] is DBNull ? null : reader["description"]
                            });
                        }
                        result["columns"] = columns;
                    }
                    // Indexes
                    using (var cmd = new NpgsqlCommand(IndexesQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TableName", name);
                        cmd.Parameters.AddWithValue("@TableSchema", schema);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var indexes = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            indexes.Add(new
                            {
                                name = reader["name"],
                                type = reader["type"],
                                description = reader["description"] is DBNull ? null : reader["description"],
                                keys = reader["keys"]
                            });
                        }
                        result["indexes"] = indexes;
                    }
                    // Constraints
                    using (var cmd = new NpgsqlCommand(ConstraintsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TableName", name);
                        cmd.Parameters.AddWithValue("@TableSchema", schema);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var constraints = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            var conType = reader["type"].ToString();
                            var typeDesc = conType switch
                            {
                                "p" => "PRIMARY KEY",
                                "u" => "UNIQUE",
                                "c" => "CHECK",
                                _ => conType
                            };
                            constraints.Add(new
                            {
                                name = reader["name"],
                                type = typeDesc,
                                keys = reader["keys"]
                            });
                        }
                        result["constraints"] = constraints;
                    }

                    // Foreign Keys
                    using (var cmd = new NpgsqlCommand(ForeignKeyInformation, conn))
                    {
                        cmd.Parameters.AddWithValue("@TableName", name);
                        cmd.Parameters.AddWithValue("@TableSchema", schema);
                        using var reader = await cmd.ExecuteReaderAsync();
                        var foreignKeys = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            foreignKeys.Add(new
                            {
                                name = reader["name"],
                                schema = reader["schema"],
                                table_name = reader["table_name"],
                                column_name = reader["column_names"],
                                referenced_schema = reader["referenced_schema"],
                                referenced_table = reader["referenced_table"],
                                referenced_column = reader["referenced_column_names"],
                            });
                        }
                        result["foreignKeys"] = foreignKeys;
                    }

                    return new DbOperationResult(success: true, data: result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DescribeTable failed: {Message}", ex.Message);
                return new DbOperationResult(success: false, error: ex.Message);
            }
        }
    }
}
