using System;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The on-hold rules shared by the On Hold tab and the separation screen (legacy
/// <c>EmployeeOnHold.PerformValidations</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeOnHoldService_Tests : AppTestBase
{
    private readonly IEmployeeOnHoldService _service;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public EmployeeOnHoldService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IEmployeeOnHoldService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateOnHold(int employeeId, DateTime start, DateTime end)
    {
        EmployeeOnHold hold = null;
        UsingDbContext(context =>
        {
            hold = context.Set<EmployeeOnHold>().Add(new EmployeeOnHold
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = start,
                EndDate = end
            }).Entity;
        });

        return hold.Id;
    }

    private async Task ValidateAsync(EmployeeOnHold hold)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            await _service.ValidateAsync(hold);
            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Should_Refuse_An_End_Date_Before_The_Start_Date()
    {
        var employeeId = CreateEmployee("OHS100");

        await Should.ThrowAsync<UserFriendlyException>(() => ValidateAsync(new EmployeeOnHold
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 4, 30)
        }));
    }

    [Fact]
    public async Task Should_Refuse_A_Hold_Overlapping_Another_For_The_Same_Employee()
    {
        var employeeId = CreateEmployee("OHS110");
        CreateOnHold(employeeId, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        await Should.ThrowAsync<UserFriendlyException>(() => ValidateAsync(new EmployeeOnHold
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 4, 30)
        }));
    }

    [Fact]
    public async Task Should_Accept_An_Adjacent_Hold()
    {
        var employeeId = CreateEmployee("OHS120");
        CreateOnHold(employeeId, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        await ValidateAsync(new EmployeeOnHold
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 4, 30)
        });
    }

    [Fact]
    public async Task Should_Not_Count_The_Hold_Being_Edited_As_Its_Own_Conflict()
    {
        var employeeId = CreateEmployee("OHS130");
        var id = CreateOnHold(employeeId, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        await ValidateAsync(new EmployeeOnHold
        {
            Id = id,
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 4, 30)
        });
    }

    [Fact]
    public async Task Should_Ignore_Another_Employees_Hold()
    {
        var employeeId = CreateEmployee("OHS140A");
        var otherId = CreateEmployee("OHS140B");
        CreateOnHold(otherId, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        await ValidateAsync(new EmployeeOnHold
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31)
        });
    }
}
