using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.PersonnelAdministration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.TrainingManagement;

/// <summary>
/// Base class for the TrainingManagement bespoke app-service unit tests. Provides a private in-memory Sqlite
/// <see cref="TrainingManagementTestDbContext"/> per test instance and a helper to construct app services
/// directly (bypassing the real Abp DI/interceptor pipeline -- so <c>[AbpAuthorize]</c> is not enforced in
/// these tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementAppServiceTestBase</c>.
/// </summary>
public abstract class TrainingManagementAppServiceTestBase : AppTestBase
{
    protected readonly TrainingManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected TrainingManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TrainingManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new TrainingManagementTestDbContext(options);
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
    /// Shared helper: inserts a minimal valid Institution directly into <see cref="Context"/>.
    /// </summary>
    protected int CreateInstitution(string nameEn = "Institution")
    {
        var entity = new HCMv3.GeneralMasterInfo.Institution
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8),
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.Institutions.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts an EmployeeCertification (+ its required Institution) directly into
    /// <see cref="Context"/>.
    /// </summary>
    protected int CreateEmployeeCertification(int employeeId, CertificationType type, string certificate = "Cert", string course = "Course")
    {
        var entity = new EmployeeCertification
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            Course = course,
            Certificate = certificate,
            InstitutionId = CreateInstitution(),
            CertificationType = type
        };
        Context.EmployeeCertifications.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid TrainingCourse directly into <see cref="Context"/>.
    /// </summary>
    protected int CreateTrainingCourse()
    {
        var entity = new HCMv3.TrainingManagement.TrainingCourse
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8),
            NameEn = "Course",
            NameAr = "Course"
        };
        Context.TrainingCourses.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>
    /// Shared helper: inserts a minimal valid TrainingSchedule (+ its required TrainingCourse) directly into
    /// <see cref="Context"/>.
    /// </summary>
    protected int CreateTrainingSchedule(DateTime startDate, DateTime endDate, int? trainingCourseId = null)
    {
        var courseId = trainingCourseId ?? CreateTrainingCourse();

        var entity = new HCMv3.TrainingManagement.TrainingSchedule
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8),
            NameEn = "Schedule",
            NameAr = "Schedule",
            TrainingCourseId = courseId,
            StartDate = startDate,
            EndDate = endDate
        };
        Context.TrainingSchedules.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }
}
