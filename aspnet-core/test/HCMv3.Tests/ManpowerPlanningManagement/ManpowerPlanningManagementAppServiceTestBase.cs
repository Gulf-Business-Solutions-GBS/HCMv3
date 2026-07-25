using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.ManpowerPlanningManagement;

/// <summary>
/// Base class for the ManpowerPlanningManagement bespoke app-service unit tests. Provides a private
/// in-memory Sqlite <see cref="ManpowerPlanningManagementTestDbContext"/> per test instance and a helper to
/// construct app services directly (bypassing the real Abp DI/interceptor pipeline -- so
/// <c>[AbpAuthorize]</c> is not enforced in these tests, only the app services' own business/validation
/// logic is exercised) with <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and
/// <c>AbpSession.TenantId</c> work. Same pattern as
/// <c>HCMv3.Tests.GovtRelationsManagement.GovtRelationsManagementAppServiceTestBase</c>.
/// </summary>
public abstract class ManpowerPlanningManagementAppServiceTestBase : AppTestBase
{
    protected readonly ManpowerPlanningManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected ManpowerPlanningManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ManpowerPlanningManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ManpowerPlanningManagementTestDbContext(options);
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

    protected long CreateOrganizationUnit(string displayName)
    {
        var entity = new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null)
        {
            Code = displayName
        };
        Context.OrganizationUnits.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateEmployee(string code)
    {
        var nationality = new Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "N" + code,
            NameAr = "N" + code
        };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var employee = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            FirstNameEn = "F",
            LastNameEn = "L",
            FirstNameAr = "ف",
            LastNameAr = "ل",
            FullNameEn = "F L " + code,
            FullNameAr = "ف ل " + code,
            NationalityId = nationality.Id,
            JoiningDate = new DateTime(2020, 1, 1)
        };
        Context.Employees.Add(employee);
        Context.SaveChanges();
        return employee.Id;
    }

    protected int CreateJobTitle(string code)
    {
        var entity = new JobTitle
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.JobTitles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }
}
