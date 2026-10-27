using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Help.Dto;

namespace HCMv3.Help;

/// <summary>
/// Serves help's technical tab: the table and column behind a field.
///
/// <para>
/// Gated on its own permission because it discloses schema. Administrators hold it automatically -- both
/// admin roles grant all permissions by default -- and nobody else does until a role is given it.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Administration_HelpTechnicalInfo)]
public class HelpMetadataAppService : HCMv3AppServiceBase, IHelpMetadataAppService
{
    /// <summary>
    /// Only the application's own entities are described. The EF model also maps ABP's and OpenIddict's
    /// infrastructure tables; none of them appears on a screen with help, so there is no reason to answer
    /// questions about them.
    /// </summary>
    private const string DescribableNamespace = "HCMv3.";

    private readonly IEntitySchemaReader _schemaReader;

    public HelpMetadataAppService(IEntitySchemaReader schemaReader)
    {
        _schemaReader = schemaReader;
    }

    public async Task<EntitySchemaDto> GetEntitySchema(string entityFullName)
    {
        var name = entityFullName?.Trim();
        if (string.IsNullOrEmpty(name) || !name.StartsWith(DescribableNamespace, StringComparison.Ordinal))
        {
            throw new UserFriendlyException(L("HelpEntityNotDescribable", entityFullName ?? string.Empty));
        }

        var schema = await _schemaReader.FindAsync(name);
        if (schema == null)
        {
            throw new UserFriendlyException(L("HelpEntityNotDescribable", name));
        }

        return MapToDto(schema);
    }

    private static EntitySchemaDto MapToDto(EntitySchema schema)
    {
        return new EntitySchemaDto
        {
            EntityFullName = schema.EntityFullName,
            TableName = schema.TableName,
            Schema = schema.Schema,
            Properties = schema.Properties
                .Select(p => new PropertySchemaDto
                {
                    PropertyName = p.PropertyName,
                    ColumnName = p.ColumnName,
                    ClrType = p.ClrType,
                    StoreType = p.StoreType,
                    IsNullable = p.IsNullable,
                    MaxLength = p.MaxLength,
                    IsPrimaryKey = p.IsPrimaryKey,
                    IsEnum = p.IsEnum,
                    ForeignKeyEntity = p.ForeignKeyEntity,
                    ForeignKeyTable = p.ForeignKeyTable,
                    ForeignKeyColumn = p.ForeignKeyColumn,
                })
                .ToList(),
        };
    }
}
