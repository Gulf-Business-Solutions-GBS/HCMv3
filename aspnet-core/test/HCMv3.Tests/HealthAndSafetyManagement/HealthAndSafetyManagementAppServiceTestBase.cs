using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.PersonnelAdministration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.HealthAndSafetyManagement;

/// <summary>
/// Base class for the HealthAndSafetyManagement bespoke app-service unit tests. Provides a private
/// in-memory Sqlite <see cref="HealthAndSafetyManagementTestDbContext"/> per test instance and a helper to
/// construct app services directly (bypassing the real Abp DI/interceptor pipeline -- so
/// <c>[AbpAuthorize]</c> is not enforced in these tests, only the app services' own business/validation
/// logic is exercised) with <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and
/// <c>AbpSession.TenantId</c> work. Same pattern as
/// <c>HCMv3.Tests.ProjectManagement.ProjectManagementAppServiceTestBase</c>.
/// </summary>
public abstract class HealthAndSafetyManagementAppServiceTestBase : AppTestBase
{
    protected readonly HealthAndSafetyManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected HealthAndSafetyManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<HealthAndSafetyManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new HealthAndSafetyManagementTestDbContext(options);
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
    /// Shared helper: inserts a minimal valid Employee (+ its required Nationality) directly into
    /// <see cref="Context"/> for tests that need an EmployeeId FK.
    /// </summary>
    protected int CreateEmployee()
    {
        var nationality = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "N",
            NameAr = "N"
        };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = "F",
            LastNameEn = "L",
            FirstNameAr = "ف",
            LastNameAr = "ل",
            NationalityId = nationality.Id,
            JoiningDate = new DateTime(2020, 1, 1)
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid WorkLocation directly into <see cref="Context"/> for tests
    /// that need a LocationId FK.
    /// </summary>
    protected int CreateWorkLocation()
    {
        var entity = new HCMv3.OrganizationManagement.WorkLocation
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "L",
            NameAr = "L"
        };
        Context.WorkLocations.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid Currency directly into <see cref="Context"/> for tests that
    /// need a CurrencyId FK.
    /// </summary>
    protected int CreateCurrency()
    {
        var entity = new HCMv3.GeneralMasterInfo.Currency
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "USD",
            NameAr = "USD"
        };
        Context.Currencies.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }
}
