using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.EntityFrameworkCore;
using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HCMv3.Help;

/// <summary>
/// <see cref="IEntitySchemaReader"/> over the live EF model.
///
/// <para>
/// Reads the RELATIONAL mapping -- <c>GetTableMappings()</c> and its column mappings -- rather than
/// property annotations. The relational model is what EF itself uses to build SQL, so the names it gives
/// are the names the database actually has, including every convention and fluent override; and unlike
/// several design-time annotations it is kept in the runtime model, so it answers in production, not
/// just under <c>dotnet ef</c>.
/// </para>
/// </summary>
public class EntitySchemaReader : IEntitySchemaReader, ITransientDependency
{
    private readonly IDbContextProvider<HCMv3DbContext> _dbContextProvider;

    public EntitySchemaReader(IDbContextProvider<HCMv3DbContext> dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<EntitySchema> FindAsync(string entityFullName)
    {
        if (string.IsNullOrWhiteSpace(entityFullName))
        {
            return null;
        }

        var context = await _dbContextProvider.GetDbContextAsync();
        var entityType = context.Model.FindEntityType(entityFullName);
        var mapping = entityType?.GetTableMappings().FirstOrDefault();
        if (mapping == null)
        {
            return null;
        }

        var primaryKey = entityType.FindPrimaryKey();

        var properties = mapping.ColumnMappings
            .Select(cm =>
            {
                var property = cm.Property;
                var foreignKey = property.GetContainingForeignKeys().FirstOrDefault();
                var bareType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                return new PropertySchema
                {
                    PropertyName = property.Name,
                    ColumnName = cm.Column.Name,
                    ClrType = FriendlyTypeName(property.ClrType),
                    StoreType = cm.Column.StoreType,
                    IsNullable = cm.Column.IsNullable,
                    MaxLength = property.GetMaxLength(),
                    IsPrimaryKey = primaryKey?.Properties.Contains(property) == true,
                    IsEnum = bareType.IsEnum,
                    ForeignKeyEntity = foreignKey?.PrincipalEntityType.ClrType.Name,
                    ForeignKeyTable = foreignKey?.PrincipalEntityType.GetTableName(),
                    ForeignKeyColumn = foreignKey == null ? null : PrincipalColumnName(foreignKey, property),
                };
            })
            .OrderBy(p => p.PropertyName, StringComparer.Ordinal)
            .ToList();

        return new EntitySchema
        {
            EntityFullName = entityType.ClrType.FullName,
            TableName = mapping.Table.Name,
            Schema = mapping.Table.Schema,
            Properties = properties,
        };
    }

    /// <summary>The principal-side column this dependent property references.</summary>
    private static string PrincipalColumnName(IForeignKey foreignKey, IProperty dependent)
    {
        var index = foreignKey.Properties.ToList().IndexOf(dependent);
        var principal = foreignKey.PrincipalKey.Properties[index];
        var table = StoreObjectIdentifier.Create(foreignKey.PrincipalEntityType, StoreObjectType.Table);
        return table.HasValue ? principal.GetColumnName(table.Value) : principal.GetColumnName();
    }

    private static string FriendlyTypeName(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            return FriendlyTypeName(underlying) + "?";
        }

        return type switch
        {
            _ when type == typeof(string) => "string",
            _ when type == typeof(int) => "int",
            _ when type == typeof(long) => "long",
            _ when type == typeof(short) => "short",
            _ when type == typeof(byte) => "byte",
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(decimal) => "decimal",
            _ when type == typeof(double) => "double",
            _ when type == typeof(float) => "float",
            _ when type == typeof(byte[]) => "byte[]",
            _ => type.Name,
        };
    }
}
