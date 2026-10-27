using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.PersonnelAdministration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.GatePassManagement;

/// <summary>
/// Base class for the GatePassManagement bespoke app-service unit tests. Provides a private in-memory Sqlite
/// <see cref="GatePassManagementTestDbContext"/> per test instance and a helper to construct app services
/// directly (bypassing the real Abp DI/interceptor pipeline -- so <c>[AbpAuthorize]</c> is not enforced in
/// these tests, only the app services' own business/validation logic is exercised) with
/// <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and <c>AbpSession.TenantId</c> work.
/// Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementAppServiceTestBase</c>.
/// </summary>
public abstract class GatePassManagementAppServiceTestBase : AppTestBase
{
    protected readonly GatePassManagementTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected GatePassManagementAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<GatePassManagementTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new GatePassManagementTestDbContext(options);
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

    /// <summary>Shared helper: inserts a minimal GatePassType directly into <see cref="Context"/>.</summary>
    protected int CreateGatePassType()
    {
        var entity = new HCMv3.GatePassManagement.GatePassType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "1",
            NameEn = "Exit",
            NameAr = "Exit"
        };
        Context.GatePassTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>Shared helper: inserts a minimal GatePassMovement directly into <see cref="Context"/>.</summary>
    protected int CreateGatePassMovement()
    {
        var entity = new HCMv3.GatePassManagement.GatePassMovement
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "1",
            NameEn = "In",
            NameAr = "In"
        };
        Context.GatePassMovements.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>Shared helper: inserts a minimal GatePass header directly into <see cref="Context"/>.</summary>
    protected int CreateGatePass()
    {
        var entity = new HCMv3.GatePassManagement.GatePass
        {
            TenantId = AbpSession.TenantId ?? 1,
            GatePassNumber = 1,
            GatePassTypeId = CreateGatePassType(),
            GatePassMovementId = CreateGatePassMovement(),
            IssueDate = new DateTime(2024, 1, 1),
            ValidUpto = new DateTime(2024, 1, 2)
        };
        Context.GatePasses.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    /// <summary>Shared helper: inserts a minimal VehicleType/Brand/Model directly into <see cref="Context"/>.</summary>
    protected (int typeId, int brandId, int modelId) CreateVehicleLookups()
    {
        var type = new HCMv3.GatePassManagement.VehicleType { TenantId = AbpSession.TenantId ?? 1, Code = "1", NameEn = "Car", NameAr = "Car" };
        var brand = new HCMv3.GatePassManagement.VehicleBrand { TenantId = AbpSession.TenantId ?? 1, Code = "1", NameEn = "Toyota", NameAr = "Toyota" };
        var model = new HCMv3.GatePassManagement.VehicleModel { TenantId = AbpSession.TenantId ?? 1, Code = "1", NameEn = "Corolla", NameAr = "Corolla" };
        Context.VehicleTypes.Add(type);
        Context.VehicleBrands.Add(brand);
        Context.VehicleModels.Add(model);
        Context.SaveChanges();
        return (type.Id, brand.Id, model.Id);
    }
}
