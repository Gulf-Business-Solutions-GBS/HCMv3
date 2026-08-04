using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.PayrollManagement.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement.Services;
using NSubstitute;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Construction harness for <see cref="TimeEngineService"/>.
///
/// <para><b>Why reflection over the constructor.</b> <c>TimeEngineService</c> constructor-injects 40+
/// <see cref="IRepository{TEntity}"/> instances plus five services. Listing them positionally in every test
/// file would make the tests break on any future constructor change that is irrelevant to them, so the
/// harness walks <see cref="ConstructorInfo.GetParameters"/> and supplies each parameter by type: an
/// <c>IRepository&lt;T&gt;</c> gets a list-backed substitute whose store the test can reach through
/// <see cref="Store{TEntity}"/>; anything else gets a bare NSubstitute double.</para>
///
/// <para><b>Why list-backed repositories.</b> Identical rationale to
/// <c>TimeRegisterFieldCalculator_Tests</c>: <c>HCMv3.Core</c> has no EF Core reference, so every query in
/// <c>TimeEngineService</c> is synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c> and a
/// <c>List&lt;T&gt;.AsQueryable()</c> is a faithful stand-in -- no async LINQ provider needed.</para>
///
/// <para><b>Why private methods are invoked reflectively.</b> The three methods under test
/// (<c>AutoAirTicketPayAsync</c>, <c>CalcMonthlyToleranceDeductionAsync</c>,
/// <c>CalculateAnnualLeaveAdjustAsync</c>) are private members of the <c>TimeEngineService</c> partial class,
/// reachable publicly only through a whole <c>CreateTimeRegisterAsync</c> run over dozens of seeded entities.
/// They each write money or quota balances (air-ticket entitlements, monthly tolerance deductions,
/// annual-leave adjustments), so they are exercised directly rather than left uncovered.</para>
/// </summary>
public sealed class TimeEngineServiceTestHarness
{
    private static readonly MethodInfo ListRepositoryFactory = typeof(TimeEngineServiceTestHarness)
        .GetMethod(nameof(CreateListRepository), BindingFlags.NonPublic | BindingFlags.Static);

    private readonly Dictionary<Type, object> _stores = new Dictionary<Type, object>();

    public TimeEngineService Service { get; }

    /// <summary>Exposed because the air-ticket port calls it for the accrual window; unconfigured it returns
    /// <c>default</c> dates, which makes <c>AutoAirTicketPayAsync</c> skip every ticket type.</summary>
    public IAccrualService AccrualService { get; } = Substitute.For<IAccrualService>();

    /// <summary>
    /// Exposed (and pre-stubbed with empty settings) because a bare NSubstitute double returns <c>null</c>
    /// from every <c>Get*SettingsAsync</c>, and <c>LoadSettingsAsync</c> dereferences four of them
    /// immediately -- so any test that drives a whole <c>CreateTimeRegisterAsync</c> run would fail with a
    /// <see cref="NullReferenceException"/> before reaching what it is actually testing.
    /// </summary>
    public ISysSettingsAccessor SysSettingsAccessor { get; } = Substitute.For<ISysSettingsAccessor>();

    public TimeEngineServiceTestHarness()
    {
        SysSettingsAccessor.GetTimeSettingsAsync().Returns(new ManageTimeSettings());
        SysSettingsAccessor.GetPayrollSettingsAsync().Returns(new ManagePayrollSettings());
        SysSettingsAccessor.GetPersonnelSettingsAsync().Returns(new ManagePersonnelSettings());
        SysSettingsAccessor.GetSecuritySettingsAsync().Returns(new ManageSecuritySettings());

        var constructor = typeof(TimeEngineService).GetConstructors().Single();
        var arguments = constructor.GetParameters().Select(p => Resolve(p.ParameterType)).ToArray();
        Service = (TimeEngineService)constructor.Invoke(arguments);
    }

    /// <summary>The backing list behind the substituted <c>IRepository&lt;TEntity&gt;</c>. Seed it before
    /// invoking, assert on it afterwards.</summary>
    public List<TEntity> Store<TEntity>() where TEntity : class, IEntity<int>
    {
        if (!_stores.TryGetValue(typeof(TEntity), out var store))
        {
            store = new List<TEntity>();
            _stores[typeof(TEntity)] = store;
        }

        return (List<TEntity>)store;
    }

    /// <summary>Seeds an entity with an explicit id (the list-backed repository only assigns ids on insert).</summary>
    public TEntity Add<TEntity>(TEntity entity, int id) where TEntity : Entity<int>
    {
        entity.Id = id;
        Store<TEntity>().Add(entity);
        return entity;
    }

    public Task InvokeAsync(string methodName, params object[] arguments)
    {
        var method = typeof(TimeEngineService)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            throw new InvalidOperationException(
                "TimeEngineService has no private method '" + methodName + "'.");
        }

        try
        {
            return (Task)method.Invoke(Service, arguments);
        }
        catch (TargetInvocationException ex)
        {
            // Unwrap so tests assert on the real exception, not the reflection wrapper.
            throw ex.InnerException ?? ex;
        }
    }

    private object Resolve(Type parameterType)
    {
        if (parameterType == typeof(IAccrualService))
        {
            return AccrualService;
        }

        if (parameterType == typeof(ISysSettingsAccessor))
        {
            return SysSettingsAccessor;
        }

        if (parameterType.IsGenericType
            && parameterType.GetGenericTypeDefinition() == typeof(IRepository<>))
        {
            var entityType = parameterType.GetGenericArguments()[0];
            var storeType = typeof(List<>).MakeGenericType(entityType);

            if (!_stores.TryGetValue(entityType, out var store))
            {
                store = Activator.CreateInstance(storeType);
                _stores[entityType] = store;
            }

            return ListRepositoryFactory.MakeGenericMethod(entityType).Invoke(null, new[] { store });
        }

        return Substitute.For(new[] { parameterType }, new object[0]);
    }

    private static IRepository<TEntity> CreateListRepository<TEntity>(List<TEntity> store)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();
        var nextId = 1000;

        repository.GetAll().Returns(_ => store.AsQueryable());

        repository.InsertAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            if (entity.Id == 0)
            {
                entity.Id = nextId++;
            }

            if (!store.Contains(entity))
            {
                store.Add(entity);
            }

            return Task.FromResult(entity);
        });

        repository.InsertAndGetIdAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            if (entity.Id == 0)
            {
                entity.Id = nextId++;
            }

            if (!store.Contains(entity))
            {
                store.Add(entity);
            }

            return Task.FromResult(entity.Id);
        });

        // The service mutates the tracked instance and calls UpdateAsync; the store already holds that same
        // reference, so there is nothing to write back.
        repository.UpdateAsync(Arg.Any<TEntity>()).Returns(call => Task.FromResult(call.Arg<TEntity>()));

        repository.DeleteAsync(Arg.Any<int>()).Returns(call =>
        {
            store.RemoveAll(r => r.Id == call.Arg<int>());
            return Task.CompletedTask;
        });

        return repository;
    }
}
