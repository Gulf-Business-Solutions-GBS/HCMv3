using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.Threading;
using Abp.UI;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetupV2;
using HCMv3.GoLiveSetupV2.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetupV2;

/// <summary>
/// Clear: taking a go-live company back to no employees and no payroll month, so the sheet can be loaded
/// again and a month -- an earlier one included -- set up again. The company's setup stays: the org unit,
/// the grade and its rules, the working time. The next load simply reuses them.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2ClearCompany_Tests : AppTestBase
{
    private const string Company = "ABC Holding";
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime September = new(2026, 9, 30);
    private static readonly DateTime August = new(2026, 8, 31);
    private static readonly DateTime Joined = new(2020, 1, 15);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2ClearCompany_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>What Clear would remove, counted, so the confirmation can say it before anything goes.</summary>
    [Fact]
    public async Task Should_Say_What_Clear_Would_Remove()
    {
        await GivenAPaidMonth(September);

        var preview = await _goLive.GetClearPreview(DefaultArea);

        preview.CompanyName.ShouldBe(Company);
        preview.Employees.ShouldBe(2);
        preview.SalaryRegisters.ShouldBe(2);
        preview.TimeRegisters.ShouldBeGreaterThan(0);
        preview.PayrollMonthEnd.ShouldBe(September);
    }

    [Fact]
    public async Task Should_Say_There_Is_Nothing_To_Clear_In_An_Empty_Area()
    {
        GivenTheReferenceData();

        var preview = await _goLive.GetClearPreview(DefaultArea);

        preview.Employees.ShouldBe(0);
        preview.PayrollMonthEnd.ShouldBeNull();
    }

    /// <summary>Clearing a company takes its name typed back: a wrong one removes nothing.</summary>
    [Fact]
    public async Task Should_Refuse_Without_The_Company_Name()
    {
        await GivenAPaidMonth(September);

        var refused = await Should.ThrowAsync<UserFriendlyException>(() =>
            _goLive.ClearCompany(new GoLiveV2ClearCompanyInput { PayrollAreaName = DefaultArea, ConfirmCompanyName = "ABC" }));

        refused.Message.ShouldBe(Localized("GoLiveV2ClearNameMismatch", Company));
        (await _goLive.GetClearPreview(DefaultArea)).Employees.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Remove_The_Employees_Their_Payroll_And_The_Payroll_Month()
    {
        await GivenAPaidMonth(September);

        var cleared = await _goLive.ClearCompany(
            new GoLiveV2ClearCompanyInput { PayrollAreaName = DefaultArea, ConfirmCompanyName = " abc holding " });

        cleared.Employees.ShouldBe(2);
        cleared.SalaryRegisters.ShouldBe(2);

        var after = await _goLive.GetClearPreview(DefaultArea);
        after.Employees.ShouldBe(0);
        after.SalaryRegisters.ShouldBe(0);
        after.TimeRegisters.ShouldBe(0);
        after.PayrollMonthEnd.ShouldBeNull();

        // Soft deletes: this raw context still sees the rows, marked deleted, which is the audit trail.
        UsingDbContext(context =>
        {
            context.Employees.Count(e => e.TenantId == TenantId && !e.IsDeleted).ShouldBe(0);
            context.Employees.Count(e => e.TenantId == TenantId && e.IsDeleted).ShouldBe(2);
            context.SalaryRegisters.Count(r => r.TenantId == TenantId && !r.IsDeleted).ShouldBe(0);
            context.PayrollControlRecords.Count(r => r.TenantId == TenantId && !r.IsDeleted).ShouldBe(0);
            context.PayrollCalendars.Count(c => c.TenantId == TenantId && !c.IsDeleted).ShouldBe(0);

            // The company's setup is kept: the next load reuses it rather than building it again.
            context.OrganizationUnits.Count(o => o.TenantId == TenantId && o.DisplayName == Company && !o.IsDeleted).ShouldBe(1);
            context.PayrollAreas.Count(a => a.TenantId == TenantId && a.Name == DefaultArea && !a.IsDeleted).ShouldBe(1);
        });
    }

    /// <summary>The point of it: the same sheet loads as new, and an earlier month can be paid.</summary>
    [Fact]
    public async Task Should_Let_The_Sheet_Load_Again_And_An_Earlier_Month_Be_Paid()
    {
        await GivenAPaidMonth(September);
        await _goLive.ClearCompany(new GoLiveV2ClearCompanyInput { PayrollAreaName = DefaultArea, ConfirmCompanyName = Company });

        var reloaded = await _goLive.LoadEmployees(Load());
        reloaded.EmployeesCreated.ShouldBe(2);
        reloaded.EmployeesAlreadyLoaded.ShouldBe(0);

        var run = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = August, PayrollAreaName = DefaultArea });

        run.Succeeded.ShouldBeTrue(run.Message);
        run.PayInDate.ShouldBe(August);
    }

    // ---------------------------------------------------------------- arrange

    private async Task GivenAPaidMonth(DateTime monthEnd)
    {
        GivenTheReferenceData();
        var load = await _goLive.LoadEmployees(Load());
        load.Rows.ShouldAllBe(r => r.Succeeded);

        var run = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = monthEnd, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);
    }

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
            OrganizationUnitName = Company,
            PayrollAreaName = DefaultArea,
            Employees = new List<GoLiveV2EmployeeRowDto>
            {
                new() { Code = "E001", FullName = "Ahmed Ali", JoiningDate = Joined, CivilId = "290011501234", BasicSalary = 500 },
                new() { Code = "E002", FullName = "Sara Khalid", JoiningDate = Joined, CivilId = "290011501235", BasicSalary = 600 },
            },
        };

    private string Localized(string key, params object[] args) =>
        string.Format(Resolve<ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName, key), args);
}
