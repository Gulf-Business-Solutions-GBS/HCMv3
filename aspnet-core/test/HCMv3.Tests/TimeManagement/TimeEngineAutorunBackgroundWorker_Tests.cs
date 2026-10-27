using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading.Timers;
using Abp.Timing;
using HCMv3.MultiTenancy;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Tests for <see cref="TimeEngineAutorunBackgroundWorker"/> -- the nightly unattended run ported from legacy
/// <c>HCM10.TimeEngine/TimeAttendanceService.RunTimeEngine</c>.
///
/// <para>The most important assertion here is the no-op one: the worker is registered unconditionally in
/// <c>HCMv3WebHostModule.PostInitialize</c> (the enabling flag is a per-tenant runtime setting and cannot be
/// evaluated at host start-up), so the ONLY thing standing between a tenant that has not opted in and a
/// full nightly Time Engine run over all of its employees is the
/// <c>ManageTimeSettings.EnableTimeEngineAutorun</c> guard inside <c>QueueRunsForTenant</c>.</para>
///
/// <para>The worker is constructed directly with substitutes rather than resolved: it is a singleton
/// <c>PeriodicBackgroundWorkerBase</c> whose <c>DoWork</c> is protected and driven by a real timer, and the
/// tests need to drive that method deterministically without starting the timer.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineAutorunBackgroundWorker_Tests
{
    private const int TenantId = 7;

    private readonly IBackgroundJobManager _backgroundJobManager = Substitute.For<IBackgroundJobManager>();
    private readonly ISysSettingsAccessor _sysSettingsAccessor = Substitute.For<ISysSettingsAccessor>();

    private readonly List<Tenant> _tenants = new List<Tenant>();
    private readonly List<PayrollControlRecord> _controlRecords = new List<PayrollControlRecord>();
    private readonly List<PayrollArea> _payrollAreas = new List<PayrollArea>();

    private readonly List<TimeEngineJobArgs> _enqueued = new List<TimeEngineJobArgs>();

    private readonly ManageTimeSettings _timeSettings = new ManageTimeSettings
    {
        TenantId = TenantId,
        EnableTimeEngineAutorun = false,
        UseActualTransInPlannedTimePolicy = false
    };

    public TimeEngineAutorunBackgroundWorker_Tests()
    {
        _backgroundJobManager
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(
                Arg.Do<TimeEngineJobArgs>(args => _enqueued.Add(args)),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(Task.FromResult("job-1"));

        _sysSettingsAccessor.GetTimeSettingsAsync().Returns(_ => Task.FromResult(_timeSettings));

        _tenants.Add(new Tenant("tenant-7", "Tenant 7") { Id = TenantId, IsActive = true });
    }

    #region << Harness >>

    private TimeEngineAutorunBackgroundWorker CreateWorker()
    {
        return new TimeEngineAutorunBackgroundWorker(
            new AbpTimer(),
            CreateUnitOfWorkManager(),
            _backgroundJobManager,
            _sysSettingsAccessor,
            ListRepository(_controlRecords),
            ListRepository(_payrollAreas),
            ListRepository(_tenants));
    }

    /// <summary>
    /// The worker wraps everything in <c>IUnitOfWorkManager.WithUnitOfWork(...)</c> and scopes each tenant
    /// with <c>Current.SetTenantId(...)</c>; both are satisfied by handing back no-op disposables, since the
    /// repositories under test are in-memory lists and need no real ambient transaction or tenant filter.
    /// </summary>
    private static IUnitOfWorkManager CreateUnitOfWorkManager()
    {
        var manager = Substitute.For<IUnitOfWorkManager>();

        var completeHandle = Substitute.For<IUnitOfWorkCompleteHandle>();
        manager.Begin().Returns(completeHandle);
        manager.Begin(Arg.Any<UnitOfWorkOptions>()).Returns(completeHandle);
        manager.Begin(Arg.Any<System.Transactions.TransactionScopeOption>()).Returns(completeHandle);

        var activeUow = Substitute.For<IActiveUnitOfWork>();
        activeUow.SetTenantId(Arg.Any<int>()).Returns(Substitute.For<IDisposable>());
        activeUow.SetTenantId(Arg.Any<int>(), Arg.Any<bool>()).Returns(Substitute.For<IDisposable>());
        manager.Current.Returns(activeUow);

        return manager;
    }

    private static IRepository<TEntity> ListRepository<TEntity>(List<TEntity> store)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();
        repository.GetAll().Returns(_ => store.AsQueryable());
        return repository;
    }

    private static void RunWorker(TimeEngineAutorunBackgroundWorker worker)
    {
        typeof(TimeEngineAutorunBackgroundWorker)
            .GetMethod("DoWork", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(worker, new object[0]);
    }

    private void SeedPayrollArea(int id, string name)
    {
        _payrollAreas.Add(new PayrollArea
        {
            Id = id,
            TenantId = TenantId,
            Code = "PA" + id,
            Name = name
        });

        _controlRecords.Add(new PayrollControlRecord
        {
            Id = 900 + id,
            TenantId = TenantId,
            PayrollAreaId = id,
            PayrollMonth = 6,
            PayrollYear = 2024
        });
    }

    #endregion

    #region << The no-op guard >>

    [Fact]
    public void Should_Be_A_No_Op_When_Autorun_Is_Disabled()
    {
        _timeSettings.EnableTimeEngineAutorun = false;
        SeedPayrollArea(1, "Monthly");

        RunWorker(CreateWorker());

        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Be_A_No_Op_When_Autorun_Is_Disabled_Even_With_Several_Payroll_Areas()
    {
        _timeSettings.EnableTimeEngineAutorun = false;
        SeedPayrollArea(1, "Monthly");
        SeedPayrollArea(2, "Weekly");
        SeedPayrollArea(3, "Daily");

        RunWorker(CreateWorker());

        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Be_A_No_Op_When_Time_Settings_Are_Missing_Entirely()
    {
        _sysSettingsAccessor.GetTimeSettingsAsync().Returns(_ => Task.FromResult<ManageTimeSettings>(null));
        SeedPayrollArea(1, "Monthly");

        RunWorker(CreateWorker());

        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Enqueue_For_An_Inactive_Tenant()
    {
        _timeSettings.EnableTimeEngineAutorun = true;
        _tenants.Single().IsActive = false;
        SeedPayrollArea(1, "Monthly");

        RunWorker(CreateWorker());

        _enqueued.ShouldBeEmpty();
    }

    #endregion

    #region << The enabled path >>

    [Fact]
    public void Should_Enqueue_One_Run_Per_Payroll_Area_When_Autorun_Is_Enabled()
    {
        _timeSettings.EnableTimeEngineAutorun = true;
        SeedPayrollArea(1, "Monthly");
        SeedPayrollArea(2, "Weekly");

        RunWorker(CreateWorker());

        _enqueued.Count.ShouldBe(2);
        _enqueued.Select(r => r.PayrollAreaName).ShouldBe(new[] { "Monthly", "Weekly" }, ignoreOrder: true);
        _enqueued.ShouldAllBe(r => r.TenantId == TenantId);
    }

    [Fact]
    public void Should_Not_Enqueue_Anything_When_No_Payroll_Control_Record_Exists()
    {
        _timeSettings.EnableTimeEngineAutorun = true;

        // A payroll area with no control record: legacy iterated PayrollControlRecord, not PayrollArea.
        _payrollAreas.Add(new PayrollArea { Id = 1, TenantId = TenantId, Code = "PA1", Name = "Monthly" });

        RunWorker(CreateWorker());

        _enqueued.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Enqueue_Once_Per_Area_Even_With_Several_Control_Records_For_It()
    {
        _timeSettings.EnableTimeEngineAutorun = true;
        SeedPayrollArea(1, "Monthly");

        // A second period for the same area -- legacy Distinct()s the area ids.
        _controlRecords.Add(new PayrollControlRecord
        {
            Id = 999,
            TenantId = TenantId,
            PayrollAreaId = 1,
            PayrollMonth = 7,
            PayrollYear = 2024
        });

        RunWorker(CreateWorker());

        _enqueued.ShouldHaveSingleItem().PayrollAreaName.ShouldBe("Monthly");
    }

    [Fact]
    public void Should_Run_Today_Only_By_Default()
    {
        _timeSettings.EnableTimeEngineAutorun = true;
        _timeSettings.UseActualTransInPlannedTimePolicy = false;
        SeedPayrollArea(1, "Monthly");

        RunWorker(CreateWorker());

        var args = _enqueued.ShouldHaveSingleItem();
        args.StartDate.ShouldBe(Clock.Now.Date);
        args.EndDate.ShouldBe(Clock.Now.Date);
    }

    [Fact]
    public void Should_Pull_The_Window_Back_One_Day_When_Using_Actual_Trans_In_Planned_Time_Policy()
    {
        // Legacy lines 37-41.
        _timeSettings.EnableTimeEngineAutorun = true;
        _timeSettings.UseActualTransInPlannedTimePolicy = true;
        SeedPayrollArea(1, "Monthly");

        RunWorker(CreateWorker());

        var args = _enqueued.ShouldHaveSingleItem();
        args.StartDate.ShouldBe(Clock.Now.Date.AddDays(-1));
        args.EndDate.ShouldBe(Clock.Now.Date);
    }

    [Fact]
    public void Should_Not_Filter_By_Employee_And_Should_Mark_The_Run_As_Not_Payroll_Initiated()
    {
        // Legacy line 47 passed an empty employee-code argument; legacy line 57 passed IsRanFromPayrollEngine
        // = "0". Both matter: the first makes the nightly run cover every employee, the second keeps the
        // ExecutionLog distinguishable from a payroll-engine-triggered run.
        _timeSettings.EnableTimeEngineAutorun = true;
        SeedPayrollArea(1, "Monthly");

        RunWorker(CreateWorker());

        var args = _enqueued.ShouldHaveSingleItem();
        args.EmployeeCodes.ShouldBeNull();
        args.IsRanFromPayrollEngine.ShouldBeFalse();
        args.CurrentUserName.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region << Per-tenant isolation >>

    [Fact]
    public void Should_Not_Let_One_Tenants_Failure_Stop_The_Others()
    {
        // Legacy swallowed every failure silently; the port logs and carries on. Proven by making the
        // settings lookup throw on the first call only.
        _timeSettings.EnableTimeEngineAutorun = true;
        _tenants.Add(new Tenant("tenant-8", "Tenant 8") { Id = 8, IsActive = true });
        SeedPayrollArea(1, "Monthly");

        var firstCall = true;
        _sysSettingsAccessor.GetTimeSettingsAsync().Returns(_ =>
        {
            if (firstCall)
            {
                firstCall = false;
                throw new InvalidOperationException("settings unavailable");
            }

            return Task.FromResult(_timeSettings);
        });

        RunWorker(CreateWorker());

        // The first tenant blew up; the second still got its run queued.
        _enqueued.ShouldHaveSingleItem().PayrollAreaName.ShouldBe("Monthly");
    }

    #endregion
}
