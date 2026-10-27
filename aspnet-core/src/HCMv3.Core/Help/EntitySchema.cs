using System.Collections.Generic;

namespace HCMv3.Help;

/// <summary>How one entity is stored. Produced by <see cref="IEntitySchemaReader"/>; read-only.</summary>
public class EntitySchema
{
    public string EntityFullName { get; init; }

    public string TableName { get; init; }

    /// <summary>Null when the table sits in the database's default schema.</summary>
    public string Schema { get; init; }

    public IReadOnlyList<PropertySchema> Properties { get; init; } = new List<PropertySchema>();
}

/// <summary>The column behind one entity property.</summary>
public class PropertySchema
{
    public string PropertyName { get; init; }

    public string ColumnName { get; init; }

    /// <summary>C#-style type name: <c>string</c>, <c>int?</c>, <c>DateTime</c>, <c>Gender</c>.</summary>
    public string ClrType { get; init; }

    /// <summary>The database type as the provider maps it, e.g. <c>nvarchar(128)</c>.</summary>
    public string StoreType { get; init; }

    public bool IsNullable { get; init; }

    public int? MaxLength { get; init; }

    public bool IsPrimaryKey { get; init; }

    public bool IsEnum { get; init; }

    /// <summary>Set when the column is a foreign key; the entity and table it points at.</summary>
    public string ForeignKeyEntity { get; init; }

    public string ForeignKeyTable { get; init; }

    public string ForeignKeyColumn { get; init; }
}
