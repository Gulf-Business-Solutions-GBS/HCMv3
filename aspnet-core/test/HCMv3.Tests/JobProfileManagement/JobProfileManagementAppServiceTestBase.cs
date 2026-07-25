using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.CompetencyDevelopment;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.JobProfileManagement;

/// <summary>
/// Base class for the JobProfileManagement bespoke app-service unit tests. Provides a private in-memory
/// Sqlite <see cref="JobProfileManagementTestDbContext"/> per test instance and a helper to construct app
/// services directly (bypassing the real Abp DI/interceptor pipeline -- so <c>[AbpAuthorize]</c> is not
/// enforced in these tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementAppServiceTestBase</c>.
/// </summary>
public abstract class JobProfileManagementAppServiceTestBase : AppTestBase
{
    protected readonly JobProfileManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected JobProfileManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<JobProfileManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new JobProfileManagementTestDbContext(options);
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
    /// Shared helper: inserts a minimal valid JobTitle directly into <see cref="Context"/> for tests that
    /// need a JobTitleId FK.
    /// </summary>
    protected int CreateJobTitle(string nameEn = "Software Engineer")
    {
        var entity = new JobTitle
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.JobTitles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid JobDescription (+ its required JobTitle) directly into
    /// <see cref="Context"/> for tests that need a JobDescriptionId FK.
    /// </summary>
    protected int CreateJobDescription(string shortDescriptionEn = null)
    {
        var jobTitleId = CreateJobTitle();
        var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);

        var entity = new HCMv3.JobProfileManagement.JobDescription
        {
            TenantId = AbpSession.TenantId ?? 1,
            JobTitleId = jobTitleId,
            ShortDescriptionEn = shortDescriptionEn ?? "Short-" + suffix,
            ShortDescriptionAr = "قصير-" + suffix,
            LongDescriptionEn = "Long-" + suffix,
            LongDescriptionAr = "طويل-" + suffix,
            Version = "1.0",
            VersionChangedOn = DateTime.Now
        };
        Context.JobDescriptions.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid Competency (+ its required CompetencyType) directly into
    /// <see cref="Context"/> for tests that need a CompetencyId FK.
    /// </summary>
    protected int CreateCompetency()
    {
        var competencyType = new CompetencyType
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Behavioral",
            NameAr = "Behavioral"
        };
        Context.CompetencyTypes.Add(competencyType);
        Context.SaveChanges();

        var entity = new Competency
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Teamwork",
            NameAr = "Teamwork",
            CompetencyTypeId = competencyType.Id,
            Active = true
        };
        Context.Competencies.Add(entity);
        Context.SaveChanges();
        return entity.Id;
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
}
