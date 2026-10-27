using System;
using System.Collections.Generic;
using Abp;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.EntityFrameworkCore;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.BusinessRules.Services;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.BusinessRules.Records;

/// <inheritdoc cref="IRuleRecordWriter"/>
/// <remarks>
/// <para>
/// Stages into the current unit of work's own DbContext, so the follow-up is part of the transaction that is
/// still open around the save that triggered it. HCMv3DbContext saves it.
/// </para>
///
/// <para>
/// <b>Row-level protection lives here</b>, on top of the table and column limits in RuleRecordSchema. A row is
/// only updatable if it belongs to the tenant of the save and has not been deleted; one that fails either test
/// is reported as not found, not as forbidden, so a rule cannot use the difference to learn that another
/// company's row exists.
/// </para>
///
/// <para>
/// Every failure is a <see cref="UserFriendlyException"/> naming the rule and the table. It aborts the save, so
/// the transaction rolls back and nothing is saved -- which is the behaviour the business asked for -- and the
/// person saving is told which rule to take to an administrator.
/// </para>
/// </remarks>
public class RuleRecordWriter : IRuleRecordWriter, ITransientDependency
{
    private readonly IDbContextProvider<HCMv3DbContext> _dbContextProvider;
    private readonly IRuleRecordSchema _schema;
    private readonly IAbpSession _session;
    private readonly ILocalizationManager _localizationManager;

    public RuleRecordWriter(
        IDbContextProvider<HCMv3DbContext> dbContextProvider,
        IRuleRecordSchema schema,
        IAbpSession session,
        ILocalizationManager localizationManager)
    {
        _dbContextProvider = dbContextProvider;
        _schema = schema;
        _session = session;
        _localizationManager = localizationManager;
    }

    public void Apply(IReadOnlyList<RuleRecordChange> changes)
    {
        if (changes == null || changes.Count == 0)
        {
            return;
        }

        var dbContext = _dbContextProvider.GetDbContext();

        foreach (var change in changes)
        {
            var table = _schema.FindTable(change.Table)
                        ?? throw Fail(change, "It is not a table rules can write.");

            var entity = change.Kind == RuleRecordChangeKind.Create
                ? NewEntity(change, table)
                : ExistingEntity(dbContext, change, table);

            foreach (var (fieldName, value) in change.Values)
            {
                var column = table.FindColumn(fieldName)
                             ?? throw Fail(change, "'" + fieldName + "' is not a field rules can set.");

                object converted;

                try
                {
                    converted = RuleValueConverter.ConvertTo(value, column.ClrType);
                }
                catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
                {
                    throw Fail(
                        change,
                        "The value " + RuleValueConverter.FormatInvariant(value) + " does not fit '" + column.Name + "': "
                        + ex.Message);
                }

                table.EntityType.GetProperty(column.Name)!.SetValue(entity, converted);
            }

            if (change.Kind == RuleRecordChangeKind.Create)
            {
                dbContext.Add(entity);
            }
        }
    }

    private object NewEntity(RuleRecordChange change, RuleRecordTable table)
    {
        object entity;

        try
        {
            entity = Activator.CreateInstance(table.EntityType);
        }
        catch (Exception ex) when (ex is MissingMethodException or MemberAccessException)
        {
            throw Fail(change, "Records of this table cannot be created by a rule.");
        }

        // The tenant is never the rule's to choose. It is the tenant of the save that ran the rule.
        switch (entity)
        {
            case IMustHaveTenant mustHaveTenant:
                mustHaveTenant.TenantId = _session.TenantId
                                          ?? throw Fail(change, "It belongs to a company, and this save is not running in one.");
                break;

            case IMayHaveTenant mayHaveTenant:
                mayHaveTenant.TenantId = _session.TenantId;
                break;
        }

        return entity;
    }

    private object ExistingEntity(HCMv3DbContext dbContext, RuleRecordChange change, RuleRecordTable table)
    {
        var keyType = table.EntityType.GetProperty("Id")!.PropertyType;
        object key;

        try
        {
            // Each branch boxed on its own. Written as one ?: expression, C# gives both branches the common type
            // long, so the int cast is widened straight back and Find is handed a long for an int key -- which EF
            // rejects outright. Every update-record action failed on exactly that.
            if (keyType == typeof(int))
            {
                key = checked((int)change.RecordId!.Value);
            }
            else
            {
                key = change.RecordId!.Value;
            }
        }
        catch (OverflowException)
        {
            throw NotFound(change);
        }

        var entity = dbContext.Find(table.EntityType, key);

        if (entity == null || !IsVisibleToThisSave(entity))
        {
            throw NotFound(change);
        }

        return entity;
    }

    /// <summary>
    /// Checked explicitly rather than trusted to the query filters, so the guarantee does not depend on whether
    /// Find happened to answer from EF's change tracker, which applies no filter at all.
    /// </summary>
    private bool IsVisibleToThisSave(object entity)
    {
        if (entity is ISoftDelete { IsDeleted: true })
        {
            return false;
        }

        return entity switch
        {
            IMustHaveTenant mustHaveTenant => mustHaveTenant.TenantId == _session.TenantId,
            IMayHaveTenant mayHaveTenant => mayHaveTenant.TenantId == _session.TenantId,
            _ => true
        };
    }

    private UserFriendlyException NotFound(RuleRecordChange change)
    {
        return Fail(change, "Record " + change.RecordId + " was not found.");
    }

    private UserFriendlyException Fail(RuleRecordChange change, string detail)
    {
        return new UserFriendlyException(_localizationManager.GetString(
            HCMv3Consts.LocalizationSourceName,
            "BusinessRuleRecordChangeFailed").Replace("{0}", change.RuleCode)
            .Replace("{1}", change.Table)
            .Replace("{2}", detail));
    }
}
