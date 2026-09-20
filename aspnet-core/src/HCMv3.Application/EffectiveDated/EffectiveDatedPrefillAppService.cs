using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.EffectiveDated.Dto;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.EffectiveDated;

/// <inheritdoc cref="IEffectiveDatedPrefillAppService"/>
/// <remarks>
/// One service for every record type in <see cref="EffectiveDatedCatalog"/>, rather than a method per screen. The
/// values are read straight off the record and returned by field name, which is the same name the screen's form uses,
/// so the screen needs no mapping of its own.
///
/// <para>What is NOT returned: the id, the dates and the audit fields. The new record is a new record, and it takes
/// the date the user typed -- copying either back would be how a "new" row silently overwrites an old one.</para>
/// </remarks>
[AbpAuthorize]
public class EffectiveDatedPrefillAppService : HCMv3AppServiceBase, IEffectiveDatedPrefillAppService
{
    /// <summary>Never copied forward. Id and audit belong to the old record; the dates come from the user.</summary>
    private static readonly HashSet<string> NeverCopied = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "TenantId", "StartDate", "EndDate", "EffectiveDate",
        "CreationTime", "CreatorUserId", "LastModificationTime", "LastModifierUserId",
        "IsDeleted", "DeleterUserId", "DeletionTime",
    };

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CopyableProperties = new();

    private readonly IIocResolver _iocResolver;

    public EffectiveDatedPrefillAppService(IIocResolver iocResolver)
    {
        _iocResolver = iocResolver;
    }

    [HttpPost]
    public async Task<EffectiveDatedPrefillDto> GetPrefill(EffectiveDatedPrefillInput input)
    {
        var record = EffectiveDatedCatalog.Find(input?.RecordType);
        if (record == null)
        {
            throw new UserFriendlyException(L("UnknownRecordType"));
        }

        if (!await PermissionChecker.IsGrantedAsync(record.ViewPermission))
        {
            throw new AbpAuthorizationException(L("YouAreNotAuthorizedForThisOperation"));
        }

        var keys = BuildKeys(record, input);

        // The generic method does the work; this resolves it for the entity type in the catalogue entry.
        var method = typeof(EffectiveDatedPrefillAppService)
            .GetMethod(nameof(LoadPreviousAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(record.EntityType);

        var task = (Task<EffectiveDatedPrefillDto>)method.Invoke(this, new object[] { record, keys, input.EffectiveDate.Date });
        return await task;
    }

    /// <summary>
    /// The latest record of that history starting on or before the date. "In force on the date" is the record with
    /// the greatest start date not after it -- the same reading the payroll engine uses.
    /// </summary>
    private async Task<EffectiveDatedPrefillDto> LoadPreviousAsync<TEntity>(
        EffectiveDatedRecord record,
        IReadOnlyList<(string Name, long Value)> keys,
        DateTime effectiveDate)
        where TEntity : class, IEntity<int>
    {
        using var repository = _iocResolver.ResolveAsDisposable<IRepository<TEntity>>();

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var dateProperty = Expression.Property(parameter, record.DateProperty);

        Expression body = Expression.LessThanOrEqual(dateProperty, Expression.Constant(effectiveDate));
        foreach (var (name, value) in keys)
        {
            var property = Expression.Property(parameter, name);
            var constant = Expression.Constant(ConvertKey(value, property.Type), property.Type);
            body = Expression.AndAlso(body, Expression.Equal(property, constant));
        }

        var where = Expression.Lambda<Func<TEntity, bool>>(body, parameter);
        var orderBy = Expression.Lambda<Func<TEntity, DateTime>>(dateProperty, parameter);

        var previous = repository.Object.GetAll()
            .Where(where)
            .OrderByDescending(orderBy)
            .FirstOrDefault();

        await Task.CompletedTask;

        if (previous == null)
        {
            return new EffectiveDatedPrefillDto { Found = false };
        }

        return new EffectiveDatedPrefillDto
        {
            Found = true,
            SourceDate = (DateTime)typeof(TEntity).GetProperty(record.DateProperty)!.GetValue(previous)!,
            Values = ReadValues(previous),
        };
    }

    /// <summary>
    /// Every plain field of the record, by name, camelCased as the screens name them. Navigation properties are left
    /// out: the screen works from ids, and dragging a whole related object back would just be noise.
    /// </summary>
    private static Dictionary<string, object> ReadValues(object entity)
    {
        var properties = CopyableProperties.GetOrAdd(entity.GetType(), type => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !NeverCopied.Contains(p.Name) && IsPlainValue(p.PropertyType))
            .ToArray());

        var values = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            values[CamelCase(property.Name)] = property.GetValue(entity);
        }

        return values;
    }

    private static bool IsPlainValue(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
               || underlying.IsEnum
               || underlying == typeof(string)
               || underlying == typeof(decimal)
               || underlying == typeof(DateTime)
               || underlying == typeof(TimeSpan)
               || underlying == typeof(Guid);
    }

    private static string CamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);

    private static IReadOnlyList<(string Name, long Value)> BuildKeys(EffectiveDatedRecord record, EffectiveDatedPrefillInput input)
    {
        var given = (input.Keys ?? new List<EffectiveDatedKeyDto>())
            .Where(k => !string.IsNullOrWhiteSpace(k.Name))
            .ToDictionary(k => k.Name, k => k.Value, StringComparer.OrdinalIgnoreCase);

        var keys = new List<(string, long)>();
        foreach (var name in record.KeyProperties)
        {
            if (!given.TryGetValue(name, out var value))
            {
                // Without every key this would read somebody else's history -- the wrong employee, or another route's
                // fares -- so it refuses rather than guessing.
                throw new UserFriendlyException(L_Static("MissingPrefillKey", name));
            }

            keys.Add((name, value));
        }

        return keys;
    }

    /// <summary>Localisation from a static helper, which cannot use the instance's <c>L</c>.</summary>
    private static string L_Static(string name, params object[] args) =>
        string.Format(Abp.Localization.LocalizationHelper.GetString(HCMv3Consts.LocalizationSourceName, name), args);

    /// <summary>
    /// A key arrives as a number, whatever the field is: an id, or a kind of thing held as an enum -- the kind of
    /// benefit an exception is for, the rate type of an overtime line. Both are turned back into the field's own type.
    /// </summary>
    private static object ConvertKey(long value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return underlying.IsEnum
            ? Enum.ToObject(underlying, value)
            : Convert.ChangeType(value, underlying);
    }
}
