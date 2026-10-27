using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using NSubstitute;

namespace HCMv3.Tests.ProjectManagement;

/// <summary>
/// List-backed <see cref="IRepository{TEntity}"/> fakes for the <c>ProjectCostEngineService</c> tests.
///
/// <para><b>Why not <see cref="FakeRepository"/>?</b> That helper binds to
/// <see cref="ProjectManagementTestDbContext"/>, which does not (and must not, per this wave's
/// no-DbContext-changes constraint) contain the ~15 Payroll/Personnel entity types the cost engine reads.
/// <c>ProjectCostEngineService</c> lives in <c>HCMv3.Core</c>, which has no EntityFrameworkCore reference,
/// so it uses only SYNCHRONOUS <c>System.Linq</c> over <c>GetAll()</c> -- a plain
/// <c>List&lt;T&gt;.AsQueryable()</c> is therefore an exact stand-in. Same reasoning as
/// <c>HCMv3.Tests.PayrollManagement.PayrollEngineFakes</c>.</para>
///
/// <para>Unlike <c>PayrollEngineFakes</c> these fakes also honour writes (Insert / InsertAndGetId / Update /
/// Delete), because the engine's whole point is the rows it produces.</para>
/// </summary>
internal static class ProjectCostEngineFakes
{
    public static IRepository<TEntity> Repo<TEntity>(params TEntity[] items)
        where TEntity : class, IEntity<int>
    {
        return Repo((IEnumerable<TEntity>)items);
    }

    public static IRepository<TEntity> Repo<TEntity>(IEnumerable<TEntity> items)
        where TEntity : class, IEntity<int>
    {
        var list = items?.ToList() ?? new List<TEntity>();
        return RepoOver(list);
    }

    /// <summary>Same as <see cref="Repo{TEntity}(IEnumerable{TEntity})"/> but over a caller-owned list, so
    /// the test can assert on rows the engine inserted.</summary>
    public static IRepository<TEntity> RepoOver<TEntity>(List<TEntity> list)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();

        repository.GetAll().Returns(_ => list.AsQueryable());
        repository.GetAllIncluding(Arg.Any<Expression<Func<TEntity, object>>[]>()).Returns(_ => list.AsQueryable());

        repository.InsertAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            AssignId(list, entity);
            list.Add(entity);
            return System.Threading.Tasks.Task.FromResult(entity);
        });

        repository.InsertAndGetIdAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            AssignId(list, entity);
            list.Add(entity);
            return System.Threading.Tasks.Task.FromResult(entity.Id);
        });

        repository.UpdateAsync(Arg.Any<TEntity>()).Returns(call =>
            System.Threading.Tasks.Task.FromResult(call.Arg<TEntity>()));

        repository.DeleteAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            list.Remove(call.Arg<TEntity>());
            return System.Threading.Tasks.Task.CompletedTask;
        });

        repository.FirstOrDefaultAsync(Arg.Any<int>()).Returns(call =>
        {
            var id = call.Arg<int>();
            return System.Threading.Tasks.Task.FromResult(list.FirstOrDefault(x => x.Id == id));
        });

        repository.FirstOrDefaultAsync(Arg.Any<Expression<Func<TEntity, bool>>>()).Returns(call =>
        {
            var predicate = call.Arg<Expression<Func<TEntity, bool>>>().Compile();
            return System.Threading.Tasks.Task.FromResult(list.FirstOrDefault(predicate));
        });

        repository.DeleteAsync(Arg.Any<Expression<Func<TEntity, bool>>>()).Returns(call =>
        {
            var predicate = call.Arg<Expression<Func<TEntity, bool>>>().Compile();
            list.RemoveAll(x => predicate(x));
            return System.Threading.Tasks.Task.CompletedTask;
        });

        return repository;
    }

    private static void AssignId<TEntity>(List<TEntity> list, TEntity entity) where TEntity : class, IEntity<int>
    {
        if (entity.Id == 0)
        {
            entity.Id = list.Count == 0 ? 1 : list.Max(x => x.Id) + 1;
        }
    }

    /// <summary>
    /// A session fixed to tenant 1 / user 1, so <c>AbpSession.GetTenantId()</c> works without booting the
    /// full ABP test module.
    /// </summary>
    public static Abp.Runtime.Session.IAbpSession Session(int tenantId = 1)
    {
        var session = Substitute.For<Abp.Runtime.Session.IAbpSession>();
        session.TenantId.Returns(tenantId);
        session.UserId.Returns(1L);
        session.MultiTenancySide.Returns(Abp.MultiTenancy.MultiTenancySides.Tenant);
        return session;
    }

    /// <summary>
    /// A localization manager whose every source echoes the key back, so <c>L("Key")</c> returns "Key".
    /// The app services under test only use <c>L(...)</c> to build exception text, so echoing is enough and
    /// avoids booting the full ABP test module (which costs ~90s per test class).
    /// </summary>
    public static Abp.Localization.ILocalizationManager LocalizationManager()
    {
        var source = Substitute.For<Abp.Localization.Sources.ILocalizationSource>();
        source.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());
        source.GetString(Arg.Any<string>(), Arg.Any<System.Globalization.CultureInfo>())
            .Returns(call => call.ArgAt<string>(0));

        var manager = Substitute.For<Abp.Localization.ILocalizationManager>();
        manager.GetSource(Arg.Any<string>()).Returns(source);
        return manager;
    }

    /// <summary>A UnitOfWorkManager whose <c>Current.SaveChangesAsync()</c> is a no-op -- the list-backed
    /// fakes are written through immediately, so there is nothing to flush.</summary>
    public static IUnitOfWorkManager UnitOfWorkManager()
    {
        var activeUnitOfWork = Substitute.For<IActiveUnitOfWork>();
        activeUnitOfWork.SaveChangesAsync().Returns(System.Threading.Tasks.Task.CompletedTask);

        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Current.Returns(activeUnitOfWork);

        return unitOfWorkManager;
    }
}
