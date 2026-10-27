using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.Recruitment;

/// <summary>
/// Base class for the Recruitment bespoke app-service unit tests. Provides a private in-memory Sqlite
/// <see cref="RecruitmentTestDbContext"/> per test instance and a helper to construct app services directly
/// (bypassing the real Abp DI/interceptor pipeline -- so <c>[AbpAuthorize]</c> is not enforced in these
/// tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementAppServiceTestBase</c>.
/// </summary>
public abstract class RecruitmentAppServiceTestBase : AppTestBase
{
    protected readonly RecruitmentTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected RecruitmentAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<RecruitmentTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new RecruitmentTestDbContext(options);
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
    /// Shared helper: inserts a minimal valid Nationality directly into <see cref="Context"/>.
    /// </summary>
    protected int CreateNationality()
    {
        var entity = new Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "N",
            NameAr = "N"
        };
        Context.Nationalities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid Employee (+ its required Nationality) directly into
    /// <see cref="Context"/> for tests that need an EmployeeId FK.
    /// </summary>
    protected int CreateEmployee()
    {
        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = "F",
            LastNameEn = "L",
            FirstNameAr = "ف",
            LastNameAr = "ل",
            NationalityId = CreateNationality(),
            JoiningDate = new DateTime(2020, 1, 1)
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid Applicant (+ its required Nationality) directly into
    /// <see cref="Context"/>.
    /// </summary>
    protected int CreateApplicant(string firstNameEn = "John", string email = null)
    {
        var entity = new HCMv3.Recruitment.Applicant
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = firstNameEn,
            FirstNameAr = "ف",
            LastNameEn = "Doe",
            LastNameAr = "دو",
            NationalityId = CreateNationality(),
            Gender = Gender.Male,
            BirthDate = new DateTime(1990, 1, 1),
            CivilID = 123456789012,
            Email = email
        };
        Context.Applicants.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid ApplicantCV (+ its required Applicant) directly into
    /// <see cref="Context"/>.
    /// </summary>
    protected int CreateApplicantCV(int? applicantId = null)
    {
        var entity = new HCMv3.Recruitment.ApplicantCV
        {
            TenantId = AbpSession.TenantId ?? 1,
            ApplicantId = applicantId ?? CreateApplicant(),
            UploadDate = new DateTime(2026, 1, 1)
        };
        Context.ApplicantCVs.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }
}
