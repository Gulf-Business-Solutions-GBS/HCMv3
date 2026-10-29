using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Finds the employee fields on a transaction entity, so a "Person on Request" step can route to any of them
/// without the field being written into the engine.
///
/// <para>
/// A field qualifies when it is an <c>int</c>/<c>int?</c> id that either sits beside an <see cref="Employee"/>
/// navigation (<c>GuarantorId</c> + <c>Guarantor</c>) or is marked <see cref="RequestPersonFieldAttribute"/>.
/// <c>EmployeeId</c> never qualifies: that is the requester, and a request does not go to its own author.
/// </para>
/// </summary>
public static class RequestPersonFieldDiscovery
{
    private const string RequesterProperty = "EmployeeId";

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<string>> Cache = new();

    /// <summary>The qualifying property names of <paramref name="entityType"/>, in declaration order.</summary>
    public static IReadOnlyList<string> For(Type entityType)
    {
        return Cache.GetOrAdd(entityType, Discover);
    }

    /// <summary>
    /// The employee id in <paramref name="propertyName"/> of <paramref name="entity"/>, read as
    /// <paramref name="entityType"/> so an EF proxy subclass resolves the same fields. Null when the field does
    /// not qualify or is empty.
    /// </summary>
    public static int? Read(Type entityType, object entity, string propertyName)
    {
        if (entity == null || !For(entityType).Contains(propertyName))
        {
            return null;
        }

        var value = entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(entity);

        return value is int id && id > 0 ? id : null;
    }

    private static IReadOnlyList<string> Discover(Type entityType)
    {
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        return properties
            .Where(p => p.Name != RequesterProperty
                        && (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
                        && p.Name.EndsWith("Id", StringComparison.Ordinal)
                        && (p.IsDefined(typeof(RequestPersonFieldAttribute), true)
                            || properties.Any(n => n.Name == p.Name.Substring(0, p.Name.Length - 2)
                                                   && n.PropertyType == typeof(Employee))))
            .Select(p => p.Name)
            .ToList();
    }
}
