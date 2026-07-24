using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.PersonnelAdministration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.PerformanceManagement;

/// <summary>
/// Base class for the PerformanceManagement bespoke app-service unit tests. Provides a private in-memory
/// Sqlite <see cref="PerformanceManagementTestDbContext"/> per test instance and a helper to construct app
/// services directly (bypassing the real Abp DI/interceptor pipeline -- so <c>[AbpAuthorize]</c> is not
/// enforced in these tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// Same pattern as <c>HCMv3.Tests.GovtRelationsManagement.GovtRelationsManagementAppServiceTestBase</c>.
/// </summary>
public abstract class PerformanceManagementAppServiceTestBase : AppTestBase
{
    protected readonly PerformanceManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected PerformanceManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PerformanceManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new PerformanceManagementTestDbContext(options);
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
    protected int CreateEmployee(string firstNameEn = "F", string lastNameEn = "L")
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
            FirstNameEn = firstNameEn,
            LastNameEn = lastNameEn,
            FirstNameAr = "ف",
            LastNameAr = "ل",
            NationalityId = nationality.Id,
            JoiningDate = new DateTime(2020, 1, 1)
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateJobTitle(string code)
    {
        var entity = new HCMv3.OrganizationManagement.JobTitle
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

    protected int CreateKpiCategory(string code)
    {
        var entity = new HCMv3.PerformanceManagement.KpiCategory
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.KpiCategories.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateKpiInfo(string code, int? kpiCategoryId = null)
    {
        var entity = new HCMv3.PerformanceManagement.KpiInfo
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            KpiCategoryId = kpiCategoryId ?? CreateKpiCategory("KC-" + code),
            Active = true
        };
        Context.KpiInfos.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreatePARating(string code)
    {
        var entity = new HCMv3.PerformanceManagement.PARating
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = "Rating " + code,
            NameAr = "Rating " + code,
            Status = true
        };
        Context.PARatings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateFormType(int maxReviewerLimit = 0)
    {
        var entity = new HCMv3.PerformanceManagement.FormType
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "FT",
            NameAr = "FT",
            MaxReviewerLimit = maxReviewerLimit
        };
        Context.FormTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreatePACycle(string code)
    {
        var entity = new HCMv3.PerformanceManagement.PACycle
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.PACycles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreatePAPeriod(string name)
    {
        var entity = new HCMv3.PerformanceManagement.PAPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Name = name
        };
        Context.PAPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreatePAForm(int paPeriodId, int employeeId)
    {
        var entity = new HCMv3.PerformanceManagement.PAForm
        {
            TenantId = AbpSession.TenantId ?? 1,
            PAPeriodId = paPeriodId,
            EmployeeId = employeeId
        };
        Context.PAForms.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreatePAEmployee(int employeeId, int paCycleId, int formTypeId)
    {
        var entity = new HCMv3.PerformanceManagement.PAEmployee
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            PACycleId = paCycleId,
            FormTypeId = formTypeId
        };
        Context.PAEmployees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }
}
