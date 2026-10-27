using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.PersonnelAdministration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.BenefitsManagement;

/// <summary>
/// Base class for the BenefitsManagement bespoke app-service unit tests. Provides a private in-memory
/// Sqlite <see cref="BenefitsManagementTestDbContext"/> per test instance and a helper to construct app
/// services directly (bypassing the real Abp DI/interceptor pipeline -- so <c>[AbpAuthorize]</c> is not
/// enforced in these tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementAppServiceTestBase</c>.
/// </summary>
public abstract class BenefitsManagementAppServiceTestBase : AppTestBase
{
    protected readonly BenefitsManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected BenefitsManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BenefitsManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new BenefitsManagementTestDbContext(options);
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

    protected int CreateInsurancePlan()
    {
        var entity = new HCMv3.BenefitsManagement.InsurancePlan
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "IP1",
            NameEn = "Plan",
            NameAr = "Plan"
        };
        Context.InsurancePlans.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateInsuranceProvider()
    {
        var entity = new HCMv3.BenefitsManagement.InsuranceProvider
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "IPR1",
            NameEn = "Provider",
            NameAr = "Provider"
        };
        Context.InsuranceProviders.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateEmployeeInsurance(int employeeId, int insurancePlanId, int insuranceProviderId,
        double insuranceAmount = 1200, double companyContribution = 600, double employeeContribution = 600)
    {
        var entity = new HCMv3.BenefitsManagement.EmployeeInsurance
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            InsurancePlanId = insurancePlanId,
            InsuranceProviderId = insuranceProviderId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            InstallmentStartDate = new DateTime(2026, 2, 1),
            InsuranceAmount = insuranceAmount,
            CompanyContribution = companyContribution,
            EmployeeContribution = employeeContribution,
            BalanceCompanyContribution = companyContribution,
            BalanceEmployeeContribution = employeeContribution
        };
        Context.EmployeeInsurances.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }
}
