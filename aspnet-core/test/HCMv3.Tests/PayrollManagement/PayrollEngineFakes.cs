using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using NSubstitute;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Minimal in-memory <see cref="IRepository{TEntity}"/> fakes for the PayrollEngine wave-P1 domain-service
/// tests.
///
/// <para><b>Why not <see cref="FakeRepository"/>?</b> That helper is bound to
/// <see cref="PayrollManagementTestDbContext"/> and exists so the app-service tests get a real async LINQ
/// provider. The wave-P1 domain services (<c>WageTypeCalculatorResolver</c>, <c>PayrollRoundingService</c>,
/// <c>PayrollRegisterStore</c>) deliberately use only SYNCHRONOUS <c>System.Linq</c> over
/// <c>IRepository&lt;T&gt;.GetAll()</c> -- <c>HCMv3.Core</c> has no EntityFrameworkCore reference, so the
/// async EF extensions are unavailable to them by design. That means a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// is a complete and exact stand-in, and the tests stay pure, fast and free of any database or DI setup.</para>
///
/// <para>Kept in a separate file (and separate type) from <see cref="FakeRepository"/> on purpose: sibling
/// porting waves are editing this folder concurrently, and adding members to the shared helper would be a
/// merge hazard.</para>
/// </summary>
internal static class PayrollEngineFakes
{
    /// <summary>
    /// An <see cref="IRepository{TEntity}"/> whose <c>GetAll()</c>/<c>GetAllIncluding()</c> project the given
    /// in-memory list. Writes (<c>UpdateAsync</c> etc.) mutate the very objects handed in, so assertions are
    /// made directly against those instances.
    /// </summary>
    public static IRepository<TEntity> Repo<TEntity>(params TEntity[] items)
        where TEntity : class, IEntity<int>
    {
        return Repo((IEnumerable<TEntity>)items);
    }

    public static IRepository<TEntity> Repo<TEntity>(IEnumerable<TEntity> items)
        where TEntity : class, IEntity<int>
    {
        var list = items?.ToList() ?? new List<TEntity>();
        var repository = Substitute.For<IRepository<TEntity>>();

        repository.GetAll().Returns(_ => list.AsQueryable());
        repository.GetAllIncluding(Arg.Any<Expression<Func<TEntity, object>>[]>()).Returns(_ => list.AsQueryable());

        return repository;
    }
}
