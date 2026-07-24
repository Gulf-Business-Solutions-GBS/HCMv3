using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.TravelManagement;

/// <summary>
/// Base class for the TravelManagement bespoke app-service unit tests. Provides a private in-memory Sqlite
/// <see cref="TravelManagementTestDbContext"/> per test instance and a helper to construct app services
/// directly (bypassing the real Abp DI/interceptor pipeline -- so <c>[AbpAuthorize]</c> is not enforced in
/// these tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementAppServiceTestBase</c>.
/// </summary>
public abstract class TravelManagementAppServiceTestBase : AppTestBase
{
    protected readonly TravelManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected TravelManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TravelManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new TravelManagementTestDbContext(options);
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

    protected int CreateWageType()
    {
        var entity = new WageType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8),
            NameEn = "WT",
            NameAr = "WT",
            PayrollOrder = 1
        };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateEmployeeGroup()
    {
        var entity = new EmployeeGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Group",
            NameAr = "Group"
        };
        Context.EmployeeGroups.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateEmployeeSubGroup(int employeeGroupId)
    {
        var entity = new EmployeeSubGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeGroupId = employeeGroupId,
            NameEn = "SubGroup",
            NameAr = "SubGroup"
        };
        Context.EmployeeSubGroups.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateTravelDestination(string nameEn)
    {
        var entity = new HCMv3.TravelManagement.TravelDestination
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.TravelDestinations.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateTravelType()
    {
        var entity = new HCMv3.TravelManagement.TravelType
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Business",
            NameAr = "Business"
        };
        Context.TravelTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateTravel(int employeeId, int travelTypeId)
    {
        var entity = new HCMv3.TravelManagement.Travel
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8),
            EmployeeId = employeeId,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 5)
        };
        Context.Travels.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }
}
