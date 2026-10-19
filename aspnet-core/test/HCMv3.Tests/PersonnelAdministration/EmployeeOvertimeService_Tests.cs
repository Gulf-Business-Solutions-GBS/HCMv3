using System;
using System.Collections.Generic;
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
/// The one-overtime-row-per-employee-per-day rule shared by the Overtime tab and the overtime batch (legacy
/// <c>EmployeeOvertime.PerformValidations</c>, "Record already exists").
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeOvertimeService_Tests : AppTestBase
{
    private static readonly DateTime Day = new(2026, 6, 1);

    private readonly IEmployeeOvertimeService _service;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public EmployeeOvertimeService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IEmployeeOvertimeService>();
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

    private int CreateOvertime(int employeeId, DateTime day)
    {
        OvertimeType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "OT",
                NameAr = "OT",
                OvertimeRate = 1.25
            }).Entity;
        });

        EmployeeOvertime row = null;
        UsingDbContext(context =>
        {
            row = context.Set<EmployeeOvertime>().Add(new EmployeeOvertime
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                EffectiveDate = day,
                OvertimeTypeId = type.Id,
                OvertimeHours = 2
            }).Entity;
        });

        return row.Id;
    }

    private async Task InUnitOfWorkAsync(Func<Task> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            await action();
            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Should_Refuse_A_Second_Row_For_The_Same_Employee_And_Day()
    {
        var employeeId = CreateEmployee("EOS100");
        CreateOvertime(employeeId, Day);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => _service.EnsureNoOvertimeOnDateAsync(employeeId, Day)));
    }

    [Fact]
    public async Task Should_Not_Count_The_Row_Being_Edited()
    {
        var employeeId = CreateEmployee("EOS110");
        var id = CreateOvertime(employeeId, Day);

        await InUnitOfWorkAsync(() => _service.EnsureNoOvertimeOnDateAsync(employeeId, Day, id));
    }

    [Fact]
    public async Task Should_Allow_Another_Day_Or_Another_Employee()
    {
        var employeeId = CreateEmployee("EOS120A");
        var otherId = CreateEmployee("EOS120B");
        CreateOvertime(employeeId, Day);

        await InUnitOfWorkAsync(() => _service.EnsureNoOvertimeOnDateAsync(employeeId, Day.AddDays(1)));
        await InUnitOfWorkAsync(() => _service.EnsureNoOvertimeOnDateAsync(otherId, Day));
    }

    [Fact]
    public async Task Should_Report_The_Days_That_Already_Have_Overtime_In_A_Range()
    {
        var employeeId = CreateEmployee("EOS130");
        CreateOvertime(employeeId, Day);
        CreateOvertime(employeeId, Day.AddDays(10));

        HashSet<(int EmployeeId, DateTime Date)> days = null;
        await InUnitOfWorkAsync(async () =>
            days = await _service.GetDaysWithOvertimeAsync(new[] { employeeId }, Day, Day.AddDays(5)));

        days.ShouldContain((employeeId, Day));
        days.Count.ShouldBe(1);
    }
}
