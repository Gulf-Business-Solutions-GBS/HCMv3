using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Base class for the PayrollManagement bespoke app-service unit tests. Provides a private in-memory Sqlite
/// <see cref="PayrollManagementTestDbContext"/> per test instance and a helper to construct app services
/// directly (bypassing the real Abp DI/interceptor pipeline — so <c>[AbpAuthorize]</c> is not enforced in
/// these tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// See <see cref="PayrollManagementTestDbContext"/> remarks for why a standalone context is used instead of
/// the real <c>HCMv3DbContext</c> (which this wave is explicitly not allowed to modify). Same pattern as
/// <c>HCMv3.Tests.TimeManagement.TimeManagementAppServiceTestBase</c>.
/// </summary>
public abstract class PayrollManagementAppServiceTestBase : AppTestBase
{
    protected readonly PayrollManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected PayrollManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PayrollManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new PayrollManagementTestDbContext(options);
        Context.Database.EnsureCreated();
    }

    protected TService CreateService<TService>(Func<TService> factory) where TService : ApplicationService
    {
        var service = factory();
        service.AbpSession = AbpSession;
        service.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();
        service.UnitOfWorkManager = CreateFakeUnitOfWorkManager();
        return service;
    }

    /// <summary>
    /// A handful of app services (e.g. <c>OrgUnitWageAccGroupsAppService</c>, mirroring
    /// <c>EmployeeOrgAssignmentsAppService</c>'s EndDate auto-chaining pattern) call
    /// <c>CurrentUnitOfWork.SaveChangesAsync()</c> to force-flush a just-inserted row before re-querying it
    /// in the same logical operation. The real Abp DI/UnitOfWork pipeline is intentionally bypassed by this
    /// bespoke test harness (see class remarks), so a minimal NSubstitute-backed
    /// <see cref="IUnitOfWorkManager"/> is wired here whose <c>Current.SaveChangesAsync()</c> simply flushes
    /// this test's own <see cref="Context"/> — not the real (unrelated) <c>HCMv3DbContext</c>.
    /// </summary>
    private IUnitOfWorkManager CreateFakeUnitOfWorkManager()
    {
        var activeUnitOfWork = Substitute.For<IActiveUnitOfWork>();
        activeUnitOfWork.SaveChangesAsync().Returns(_ => Context.SaveChangesAsync());

        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Current.Returns(activeUnitOfWork);

        return unitOfWorkManager;
    }
}
