using System;
using Abp.Application.Services;
using Abp.Localization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Base class for the TimeManagement bespoke app-service unit tests. Provides a private in-memory Sqlite
/// <see cref="TimeManagementTestDbContext"/> per test instance and a helper to construct app services
/// directly (bypassing the real Abp DI/interceptor pipeline — so <c>[AbpAuthorize]</c> is not enforced in
/// these tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// See <see cref="TimeManagementTestDbContext"/> remarks for why a standalone context is used instead of
/// the real <c>HCMv3DbContext</c> (which this wave is explicitly not allowed to modify).
/// </summary>
public abstract class TimeManagementAppServiceTestBase : AppTestBase
{
    protected readonly TimeManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected TimeManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TimeManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new TimeManagementTestDbContext(options);
        Context.Database.EnsureCreated();
    }

    protected TService CreateService<TService>(Func<TService> factory) where TService : ApplicationService
    {
        var service = factory();
        service.AbpSession = AbpSession;
        service.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();
        return service;
    }
}
