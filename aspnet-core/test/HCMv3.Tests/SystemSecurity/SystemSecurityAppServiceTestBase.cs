using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.SystemSecurity;

/// <summary>
/// Base class for the SystemSecurity bespoke app-service unit tests. Same pattern as
/// HCMv3.Tests.TrainingManagement.TrainingManagementAppServiceTestBase.
/// </summary>
public abstract class SystemSecurityAppServiceTestBase : AppTestBase
{
    protected readonly SystemSecurityTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected SystemSecurityAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SystemSecurityTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new SystemSecurityTestDbContext(options);
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

    protected int CreateEmployee()
    {
        var nationality = new Nationality
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

    protected int CreateNotificationConfig()
    {
        var entity = new NotificationConfig
        {
            TenantId = AbpSession.TenantId ?? 1,
            Name = "Config " + Guid.NewGuid().ToString("N").Substring(0, 6)
        };
        Context.NotificationConfigs.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateEmailTemplate()
    {
        var entity = new HCMv3.SystemSecurity.EmailTemplate
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Template " + Guid.NewGuid().ToString("N").Substring(0, 6),
            NameAr = "Template"
        };
        Context.EmailTemplates.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateRole(string name = "TestRole")
    {
        var role = new HCMv3.Authorization.Roles.Role(AbpSession.TenantId, name, name);
        Context.Roles.Add(role);
        Context.SaveChanges();
        return role.Id;
    }

    protected long CreateOrganizationUnit(string displayName = "TestUnit")
    {
        var entity = new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null)
        {
            Code = displayName
        };
        Context.OrganizationUnits.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateOrganizationUnitType()
    {
        var entity = new HCMv3.OrganizationManagement.OrganizationUnitType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = HCMv3.OrganizationManagement.OrganizationUnitType.Level0Code,
            NameEn = "Level 0",
            NameAr = "Level 0"
        };
        Context.OrganizationUnitTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateTaskEvent()
    {
        var entity = new TaskEvent
        {
            TenantId = AbpSession.TenantId ?? 1,
            Subject = "Task " + Guid.NewGuid().ToString("N").Substring(0, 6),
            DueDate = DateTime.Now.AddDays(7),
            StartDate = DateTime.Now
        };
        Context.TaskEvents.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    protected int CreateEmailNotificationLog()
    {
        var entity = new EmailNotificationLog
        {
            TenantId = AbpSession.TenantId ?? 1,
            Date = DateTime.Now,
            Email = "test@example.com",
            IsSuccessful = true
        };
        Context.EmailNotificationLogs.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }
}
