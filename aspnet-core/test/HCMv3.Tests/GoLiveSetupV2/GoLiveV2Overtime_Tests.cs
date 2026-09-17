using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetupV2;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetupV2;

/// <summary>
/// Overtime on a V2 tenant: the settings the load makes give a working day 8 hours -- the divisor of the overtime
/// hourly rate -- and an overtime entry whose type is linked to its wage type is paid by the next run. Made-up
/// figures only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2Overtime_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime September = new(2026, 9, 30);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2Overtime_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    [Fact]
    public async Task Should_Give_A_Working_Day_Eight_Hours()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load());

        UsingDbContext(context => context.ManagePayrollSettingses.Where(s => s.TenantId == TenantId).Select(s => s.DefaultWorkingHours).ToList())
            .ShouldAllBe(hours => hours == GoLiveV2WorkingTime.DefaultWorkingHours);
    }

    [Fact]
    public async Task Should_Pay_Overtime_Linked_To_Its_Wage_Type()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load());

        var overtimeWageTypeId = UsingDbContext(context => context.WageTypes.Single(w => w.TenantId == TenantId && w.Code == "1007" && !w.IsDeleted).Id);
        await Resolve<IOvertimeTypesAppService>().CreateOrEdit(new CreateOrEditOvertimeTypeDto
        {
            Code = "OT1",
            NameEn = "OT on Normal Day",
            NameAr = "OT on Normal Day",
            OvertimeRate = 1.25,
            WageTypeId = overtimeWageTypeId,
        });
        var overtimeTypeId = UsingDbContext(context => context.OvertimeTypes.Single(t => t.TenantId == TenantId && t.Code == "OT1").Id);
        var employeeId = UsingDbContext(context => context.Employees.Single(e => e.Code == "E001" && !e.IsDeleted).Id);
        await Resolve<IEmployeeOvertimesAppService>().CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employeeId,
            EffectiveDate = new DateTime(2026, 9, 1),
            OvertimeTypeId = overtimeTypeId,
            OvertimeHours = 2,
            ActualOvertimeHours = 2,
            ApprovedOvertimeHours = 2,
        });

        var run = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);

        var register = await _goLive.GetSalaryRegister(new GoLiveV2SalaryRegisterInput { PayDate = September, EmployeeId = employeeId });
        var overtime = register.Lines.Where(l => l.WageCode == "1007").ToList();
        overtime.Sum(l => l.Number).ShouldBe(2.5, 0.0001);
        overtime.Sum(l => l.Amount).ShouldBeGreaterThan(0);
    }

    // ---------------------------------------------------------------- arrange

    private void GivenTheReferenceData()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }
    }

    private static GoLiveV2LoadEmployeesInput Load() =>
        new()
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = DefaultArea,
            Employees = new()
            {
                new GoLiveV2EmployeeRowDto
                {
                    Code = "E001",
                    FullName = "Employee E001",
                    JoiningDate = new DateTime(2020, 1, 15),
                    CivilId = "290011501000",
                    BasicSalary = 520,
                },
            },
        };
}
