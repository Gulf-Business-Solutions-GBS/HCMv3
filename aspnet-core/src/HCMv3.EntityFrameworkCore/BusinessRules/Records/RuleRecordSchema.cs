using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HCMv3.BusinessRules.Records;

/// <inheritdoc cref="IRuleRecordSchema"/>
/// <remarks>
/// <para>
/// Read from the EF model rather than a hand-maintained list, so "any business table" stays true as tables are
/// added -- a list would be out of date the first time somebody forgot it existed. The cost of that is that the
/// exclusions below are the whole of the protection, which is why each one says what it is protecting.
/// </para>
///
/// <para>
/// Built once and cached: the model is fixed for the life of the process and identical for every tenant.
/// </para>
/// </remarks>
public class RuleRecordSchema : IRuleRecordSchema, ISingletonDependency
{
    /// <summary>
    /// Tables by name prefix. Everything ABP keeps -- users, roles, user-roles, permissions, settings, tenants,
    /// editions, organization units, audit logs, languages, notifications, background jobs, webhooks -- starts
    /// with "Abp", and the token store starts with "OpenIddict". A rule that can write any of these can grant
    /// itself access, so none of them is writable, whatever else "any table" means.
    /// </summary>
    private static readonly string[] ExcludedTablePrefixes = { "Abp", "OpenIddict" };

    /// <summary>
    /// Entities by namespace, for the security and billing tables that do not carry an Abp prefix, and for the
    /// rule engine's own tables -- a rule that rewrites rules, or their execution log, is not a rule.
    /// </summary>
    private static readonly string[] ExcludedNamespacePrefixes =
    {
        "Abp.",
        "OpenIddict",
        "HCMv3.Authorization",
        "HCMv3.MultiTenancy",
        "HCMv3.Editions",
        "HCMv3.Auditing",
        "HCMv3.BusinessRules"
    };

    /// <summary>
    /// Columns no rule may set on any table. Id and the audit stamps are the framework's. TenantId is excluded
    /// because the tenant always comes from the save that ran the rule -- a rule able to set it could move a
    /// row into another company. WorkflowStatus and WorkflowRequestId are the approval gate: setting the status
    /// to Approved would skip the approval chain entirely.
    /// </summary>
    private static readonly HashSet<string> ExcludedColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id",
        "TenantId",
        "CreationTime",
        "CreatorUserId",
        "LastModificationTime",
        "LastModifierUserId",
        "IsDeleted",
        "DeleterUserId",
        "DeletionTime",
        "ConcurrencyStamp",
        "ExtensionData",
        "WorkflowStatus",
        "WorkflowRequestId"
    };

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IDbContextProvider<HCMv3DbContext> _dbContextProvider;
    private readonly object _buildLock = new();

    private IReadOnlyList<RuleRecordTable> _tables;
    private Dictionary<string, RuleRecordTable> _tablesByName;

    public RuleRecordSchema(IUnitOfWorkManager unitOfWorkManager, IDbContextProvider<HCMv3DbContext> dbContextProvider)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _dbContextProvider = dbContextProvider;
    }

    public IReadOnlyList<RuleRecordTable> GetTables()
    {
        EnsureBuilt();
        return _tables;
    }

    public RuleRecordTable FindTable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        EnsureBuilt();
        return _tablesByName.TryGetValue(name.Trim(), out var table) ? table : null;
    }

    private void EnsureBuilt()
    {
        if (_tables != null)
        {
            return;
        }

        lock (_buildLock)
        {
            if (_tables != null)
            {
                return;
            }

            // Joins the caller's unit of work when there is one -- a validator inside an app service, a writer
            // inside a save -- and opens one only to reach the model when there is not. No query runs.
            using var unitOfWork = _unitOfWorkManager.Begin();
            var model = _dbContextProvider.GetDbContext().Model;

            var tables = Build(model);

            _tablesByName = tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
            _tables = tables;

            unitOfWork.Complete();
        }
    }

    private static List<RuleRecordTable> Build(IModel model)
    {
        var candidates = model.GetEntityTypes()
            .Where(IsWritableEntity)
            .ToList();

        // Two entities with the same class name in different namespaces would make a table name ambiguous.
        // Rather than silently letting one shadow the other, both fall back to their full name.
        var ambiguousNames = candidates
            .GroupBy(e => e.ClrType.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return candidates
            .Select(entityType =>
            {
                var name = ambiguousNames.Contains(entityType.ClrType.Name)
                    ? entityType.ClrType.FullName
                    : entityType.ClrType.Name;

                return new RuleRecordTable(name, entityType.ClrType, BuildColumns(entityType));
            })
            .Where(table => table.Columns.Count > 0)
            .OrderBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsWritableEntity(IEntityType entityType)
    {
        var clrType = entityType.ClrType;

        if (clrType == null || clrType.IsAbstract || entityType.IsOwned() || entityType.HasSharedClrType)
        {
            return false;
        }

        // Keyless types and views have no table to insert into.
        var tableName = entityType.GetTableName();

        if (string.IsNullOrEmpty(tableName) || entityType.FindPrimaryKey() == null)
        {
            return false;
        }

        if (ExcludedTablePrefixes.Any(prefix => tableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var ns = clrType.Namespace ?? string.Empty;

        if (ExcludedNamespacePrefixes.Any(prefix => ns.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return false;
        }

        // An update names its row by a single numeric id; a composite or non-numeric key cannot be addressed.
        var key = entityType.FindPrimaryKey().Properties;

        return key.Count == 1
               && key[0].Name == "Id"
               && (key[0].ClrType == typeof(int) || key[0].ClrType == typeof(long));
    }

    private static List<RuleRecordColumn> BuildColumns(IEntityType entityType)
    {
        var columns = new List<RuleRecordColumn>();

        foreach (var property in entityType.GetProperties())
        {
            // A column the rule could set but the entity class cannot receive is not a column it can set.
            if (property.IsShadowProperty() || property.PropertyInfo == null || !property.PropertyInfo.CanWrite)
            {
                continue;
            }

            if (ExcludedColumns.Contains(property.Name) || property.IsPrimaryKey() || property.IsConcurrencyToken)
            {
                continue;
            }

            // Computed and database-generated columns are written by the database, not by a rule.
            if (property.ValueGenerated != ValueGenerated.Never)
            {
                continue;
            }

            var dataType = MapDataType(property.ClrType);

            if (dataType == RuleDataType.Unknown)
            {
                continue;
            }

            columns.Add(new RuleRecordColumn(property.Name, property.ClrType, dataType, property.IsNullable));
        }

        return columns.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Only the types a rule expression can produce. Guids, binary blobs, time spans and offsets have no rule
    /// representation, so they are left out rather than exposed as columns every value would fail to fit.
    /// </summary>
    private static RuleDataType MapDataType(Type clrType)
    {
        var type = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (type.IsEnum)
        {
            return RuleDataType.Enum;
        }

        if (type == typeof(string))
        {
            return RuleDataType.String;
        }

        if (type == typeof(bool))
        {
            return RuleDataType.Boolean;
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
        {
            return RuleDataType.Integer;
        }

        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            return RuleDataType.Decimal;
        }

        if (type == typeof(DateTime))
        {
            return RuleDataType.DateTime;
        }

        return RuleDataType.Unknown;
    }
}
