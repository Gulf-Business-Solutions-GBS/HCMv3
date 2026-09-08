using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// Loads the entity at the far end of a declared reference.
///
/// <para>
/// This exists because lazy loading is switched off in this solution. A rule that says
/// <c>Leave.Employee.Nationality.NameEn</c> cannot read <c>absence.Employee</c> and hope it was included --
/// outside the one query that happened to Include it, that property is null, and a null read makes the
/// condition quietly false. So the engine fetches by key instead, and the fetch is explicit and countable.
/// </para>
///
/// <para><b>The cost, stated plainly.</b> One keyed lookup per distinct entity per rule execution, memoised
/// on the execution context. A rule reading three fields off <c>Leave.Employee</c> loads the employee once.
/// A BeforeSave rule that walks two references costs two extra lookups on that save. That is affordable for
/// the handful of rules a tenant arms; it would not be affordable as a general-purpose ORM, which is why
/// references have to be DECLARED rather than followed freely.</para>
/// </summary>
public interface IRuleEntityLoader
{
    /// <summary>
    /// The entity of that type with that id, or null when it does not exist. Throws when the type has no
    /// repository -- a reference the engine cannot resolve is a wiring bug, and returning null would
    /// disguise it as missing data.
    /// </summary>
    object Load(Type entityType, int id);
}

/// <inheritdoc cref="IRuleEntityLoader"/>
/// <remarks>
/// <para>
/// Reflection over <c>IRepository&lt;T&gt;</c> because the entity type is only known at run time -- it comes
/// from a descriptor, not from a generic parameter. The <c>MethodInfo</c> is cached per type so the
/// reflection cost is paid once per process rather than once per read.
/// </para>
/// <para>
/// Reads go through the ordinary repository, so ABP's tenant filter and soft-delete filter still apply.
/// That is deliberate: a rule must not be able to see another tenant's rows by walking a reference, and the
/// filters are what guarantee it.
/// </para>
/// </remarks>
public class RuleEntityLoader : IRuleEntityLoader, ITransientDependency
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> FirstOrDefaultMethods = new();

    private readonly IIocResolver _iocResolver;

    public RuleEntityLoader(IIocResolver iocResolver)
    {
        _iocResolver = iocResolver;
    }

    public object Load(Type entityType, int id)
    {
        if (entityType == null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        if (!typeof(IEntity<int>).IsAssignableFrom(entityType))
        {
            throw new RuleDefinitionException(
                "'" + entityType.Name + "' cannot be used as a rule reference target because it is not an "
                + "entity with an integer key.");
        }

        var repositoryType = typeof(IRepository<>).MakeGenericType(entityType);

        if (!_iocResolver.IsRegistered(repositoryType))
        {
            throw new RuleDefinitionException(
                "No repository is registered for '" + entityType.Name + "', so a rule cannot follow a "
                + "reference to it.");
        }

        using var repository = _iocResolver.ResolveAsDisposable(repositoryType);

        var method = FirstOrDefaultMethods.GetOrAdd(entityType, _ => FindFirstOrDefault(repositoryType));

        if (method == null)
        {
            throw new RuleDefinitionException(
                "The repository for '" + entityType.Name + "' has no FirstOrDefault(int) method.");
        }

        return method.Invoke(repository.Object, new object[] { id });
    }

    /// <summary>
    /// Finds <c>FirstOrDefault(int)</c> on the repository interface or any interface it inherits.
    ///
    /// <para>
    /// The inherited search is the whole point: <c>Type.GetMethod</c> on an INTERFACE does not look at base
    /// interfaces, and <c>FirstOrDefault(TPrimaryKey)</c> is declared on
    /// <c>IRepository&lt;TEntity, TPrimaryKey&gt;</c>, not on <c>IRepository&lt;TEntity&gt;</c>. Searching
    /// only the closed type finds nothing and every reference in every rule fails.
    /// </para>
    /// </summary>
    private static MethodInfo FindFirstOrDefault(Type repositoryType)
    {
        var method = repositoryType.GetMethod(nameof(IRepository<Entity>.FirstOrDefault), new[] { typeof(int) });

        if (method != null)
        {
            return method;
        }

        return repositoryType
            .GetInterfaces()
            .Select(i => i.GetMethod(nameof(IRepository<Entity>.FirstOrDefault), new[] { typeof(int) }))
            .FirstOrDefault(m => m != null);
    }
}
