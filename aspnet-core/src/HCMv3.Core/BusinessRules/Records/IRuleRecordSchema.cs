using System;
using System.Collections.Generic;

namespace HCMv3.BusinessRules.Records;

/// <summary>
/// The tables and columns an AfterSave rule may create or update, derived from the data model.
///
/// <para>
/// <b>"Any table" means any BUSINESS table.</b> The business asked for rules to reach any table and field, and
/// that is what this exposes, with three exceptions that are not negotiable in a multi-tenant product:
/// </para>
/// <list type="bullet">
/// <item><description>The framework and security tables -- users, roles, permissions, settings, tenants,
/// editions, audit logs, OpenIddict. A rule able to insert a user-role row lets anyone who can author rules
/// make themselves an administrator.</description></item>
/// <item><description>The rule engine's own tables. A rule that rewrites rules is not a rule.</description></item>
/// <item><description>System columns -- Id, TenantId, and the creation, modification and deletion stamps. The
/// tenant in particular always comes from the save that triggered the rule.</description></item>
/// </list>
///
/// <para>
/// Declared in Core, implemented in EntityFrameworkCore: only that layer can read the data model, and Core must
/// not depend on it.
/// </para>
/// </summary>
public interface IRuleRecordSchema
{
    /// <summary>Every table a rule may write, ordered by name.</summary>
    IReadOnlyList<RuleRecordTable> GetTables();

    /// <summary>The table with this entity name (case-insensitive), or null when it does not exist or is excluded.</summary>
    RuleRecordTable FindTable(string name);
}

/// <summary>A table a rule may write, named by its entity.</summary>
public class RuleRecordTable
{
    public RuleRecordTable(string name, Type entityType, IReadOnlyList<RuleRecordColumn> columns)
    {
        Name = name;
        EntityType = entityType;
        Columns = columns;
    }

    /// <summary>The entity name, e.g. <c>EmployeeAbsAdjustment</c>.</summary>
    public string Name { get; }

    public Type EntityType { get; }

    public IReadOnlyList<RuleRecordColumn> Columns { get; }

    public RuleRecordColumn FindColumn(string name)
    {
        foreach (var column in Columns)
        {
            if (string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return column;
            }
        }

        return null;
    }
}

/// <summary>A column a rule may set.</summary>
public class RuleRecordColumn
{
    public RuleRecordColumn(string name, Type clrType, RuleDataType dataType, bool isNullable)
    {
        Name = name;
        ClrType = clrType;
        DataType = dataType;
        IsNullable = isNullable;
    }

    public string Name { get; }

    public Type ClrType { get; }

    public RuleDataType DataType { get; }

    public bool IsNullable { get; }
}
