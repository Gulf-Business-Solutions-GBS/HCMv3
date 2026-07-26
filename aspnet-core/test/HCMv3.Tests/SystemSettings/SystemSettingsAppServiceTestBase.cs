using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.SystemSettings;

/// <summary>
/// Base class for the SystemSettings bespoke app-service unit tests. Provides a private in-memory Sqlite
/// <see cref="SystemSettingsTestDbContext"/> per test instance and a helper to construct app services
/// directly (bypassing the real Abp DI/interceptor pipeline -- so <c>[AbpAuthorize]</c> is not enforced in
/// these tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// Same pattern as <c>HCMv3.Tests.TrainingManagement.TrainingManagementAppServiceTestBase</c>.
/// </summary>
public abstract class SystemSettingsAppServiceTestBase : AppTestBase
{
    protected readonly SystemSettingsTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected SystemSettingsAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SystemSettingsTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new SystemSettingsTestDbContext(options);
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

    private IUnitOfWorkManager CreateFakeUnitOfWorkManager()
    {
        var activeUnitOfWork = Substitute.For<IActiveUnitOfWork>();
        activeUnitOfWork.SaveChangesAsync().Returns(_ => Context.SaveChangesAsync());

        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Current.Returns(activeUnitOfWork);

        return unitOfWorkManager;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid Country directly into <see cref="Context"/> for tests that need
    /// a CountryId FK (see <c>PayrollDriverAppService_Tests</c>).
    /// </summary>
    protected int CreateCountry(string nameEn = "Country")
    {
        var entity = new HCMv3.GeneralMasterInfo.Country
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.Countries.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }
}
