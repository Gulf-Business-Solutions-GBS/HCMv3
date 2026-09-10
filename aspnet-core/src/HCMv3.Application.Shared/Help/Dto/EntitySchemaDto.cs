using System.Collections.Generic;

namespace HCMv3.Help.Dto;

/// <summary>How one entity is stored, for help's technical tab.</summary>
public class EntitySchemaDto
{
    public string EntityFullName { get; set; }

    public string TableName { get; set; }

    public string Schema { get; set; }

    public List<PropertySchemaDto> Properties { get; set; } = new();
}

/// <summary>The column behind one entity property.</summary>
public class PropertySchemaDto
{
    public string PropertyName { get; set; }

    public string ColumnName { get; set; }

    public string ClrType { get; set; }

    public string StoreType { get; set; }

    public bool IsNullable { get; set; }

    public int? MaxLength { get; set; }

    public bool IsPrimaryKey { get; set; }

    public bool IsEnum { get; set; }

    public string ForeignKeyEntity { get; set; }

    public string ForeignKeyTable { get; set; }

    public string ForeignKeyColumn { get; set; }
}
