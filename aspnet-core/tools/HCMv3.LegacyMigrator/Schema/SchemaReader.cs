using Microsoft.Data.SqlClient;

namespace HCMv3.LegacyMigrator.Schema;

public sealed class ColumnInfo
{
    public string Name { get; init; }
    public string SqlType { get; init; }
    public bool IsNullable { get; init; }
    public bool IsIdentity { get; init; }
    public bool IsComputed { get; init; }
    public int MaxLength { get; init; }
    public int Precision { get; init; }
    public int Scale { get; init; }

    /// <summary>Fully-qualified type usable in a CAST, e.g. <c>nvarchar(256)</c>, <c>decimal(18,2)</c>.</summary>
    public string CastType
    {
        get
        {
            switch (SqlType)
            {
                case "nvarchar":
                case "nchar":
                    return MaxLength < 0 ? SqlType + "(max)" : $"{SqlType}({MaxLength / 2})";
                case "varchar":
                case "char":
                case "varbinary":
                case "binary":
                    return MaxLength < 0 ? SqlType + "(max)" : $"{SqlType}({MaxLength})";
                case "decimal":
                case "numeric":
                    return $"{SqlType}({Precision},{Scale})";
                case "datetime2":
                case "time":
                case "datetimeoffset":
                    return $"{SqlType}({Scale})";
                default:
                    return SqlType;
            }
        }
    }
}

public sealed class TableInfo
{
    public string Name { get; init; }
    public Dictionary<string, ColumnInfo> Columns { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ColumnInfo> Ordered { get; init; } = new();

    /// <summary>column name -&gt; referenced table name (single-column FKs only).</summary>
    public Dictionary<string, string> ForeignKeys { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Has(string column) => Columns.ContainsKey(column);
    public ColumnInfo Get(string column) => Columns.TryGetValue(column, out var c) ? c : null;
}

/// <summary>
/// Reads table/column/FK metadata from a database. Used on BOTH sides so column mapping can be derived
/// by convention at run time rather than hard-coded per table -- which is what makes this tool reusable
/// for a different legacy customer database whose column set differs.
/// </summary>
public sealed class SchemaReader
{
    private const string ColumnSql = @"
SELECT t.name AS TableName,
       c.name AS ColumnName,
       ty.name AS TypeName,
       c.is_nullable,
       c.is_identity,
       c.is_computed,
       c.max_length,
       c.precision,
       c.scale,
       c.column_id
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE SCHEMA_NAME(t.schema_id) = 'dbo'
ORDER BY t.name, c.column_id;";

    private const string ForeignKeySql = @"
SELECT OBJECT_NAME(fk.parent_object_id)      AS ParentTable,
       pc.name                               AS ParentColumn,
       OBJECT_NAME(fk.referenced_object_id)  AS ReferencedTable
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
WHERE fk.object_id NOT IN (
    SELECT constraint_object_id FROM sys.foreign_key_columns GROUP BY constraint_object_id HAVING COUNT(*) > 1
);";

    public static Dictionary<string, TableInfo> Read(string connectionString, bool includeForeignKeys)
    {
        var tables = new Dictionary<string, TableInfo>(StringComparer.OrdinalIgnoreCase);

        using var conn = new SqlConnection(connectionString);
        conn.Open();

        using (var cmd = new SqlCommand(ColumnSql, conn) { CommandTimeout = 300 })
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                var tableName = r.GetString(0);
                if (!tables.TryGetValue(tableName, out var table))
                {
                    table = new TableInfo { Name = tableName };
                    tables[tableName] = table;
                }

                var col = new ColumnInfo
                {
                    Name = r.GetString(1),
                    SqlType = r.GetString(2),
                    IsNullable = r.GetBoolean(3),
                    IsIdentity = r.GetBoolean(4),
                    IsComputed = r.GetBoolean(5),
                    MaxLength = r.GetInt16(6),
                    Precision = r.GetByte(7),
                    Scale = r.GetByte(8)
                };

                table.Columns[col.Name] = col;
                table.Ordered.Add(col);
            }
        }

        if (includeForeignKeys)
        {
            using var cmd = new SqlCommand(ForeignKeySql, conn) { CommandTimeout = 300 };
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var parent = r.GetString(0);
                if (tables.TryGetValue(parent, out var table))
                {
                    table.ForeignKeys[r.GetString(1)] = r.GetString(2);
                }
            }
        }

        return tables;
    }
}
