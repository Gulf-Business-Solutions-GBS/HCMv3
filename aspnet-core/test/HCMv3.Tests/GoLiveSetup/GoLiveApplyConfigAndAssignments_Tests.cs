using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// The two stages that make the difference between "the data loaded" and "a payroll will actually run":
/// S1.1 turns the decision steps into a payroll area and an annual leave type, and S3.1's second pass
/// attaches every employee to that payroll area.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveApplyConfigAndAssignments_Tests : AppTestBase
{
    private readonly IGoLiveApplyAppService _apply;
    private readonly IGoLiveStateAppService _state;

    public GoLiveApplyConfigAndAssignments_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _apply = Resolve<IGoLiveApplyAppService>();
        _state = Resolve<IGoLiveStateAppService>();

        var seeder = Resolve<IReferenceDataSeeder>();
        var tenantId = AbpSession.TenantId ?? 1;
        AsyncHelper.RunSync(() => seeder.SeedPackAsync("universal", tenantId));
        AsyncHelper.RunSync(() => seeder.SeedPackAsync("kuwait", tenantId));
    }

    private Task SaveRows(string stepId, params string[] rows) =>
        _state.SaveStep(new SaveGoLiveStepInput
        {
            StepId = stepId,
            Payload = "{\"rows\":[" + string.Join(",", rows) + "]}",
        });

    private Task SaveAnswers(string stepId, params string[] pairs) =>
        _state.SaveStep(new SaveGoLiveStepInput
        {
            StepId = stepId,
            Payload = "{\"answers\":{" + string.Join(",", pairs) + "}}",
        });

    /// <summary>The minimum that makes the config stage produce a payroll area.</summary>
    private Task SaveCompany(
        string name = "Gulf Business Solutions",
        string currency = "Kuwaiti Dinar",
        string fiscalStart = "January") =>
        SaveAnswers(
            "S1.1",
            $@"""Q1.01"":""{name}""",
            $@"""Q1.04"":""{currency}""",
            $@"""Q1.07"":""{fiscalStart}""");

    private Task SaveEmployee(string code, string jobTitle = null, string workLocation = null)
    {
        var extra = jobTitle == null ? string.Empty : $@",""jobTitle"":""{jobTitle}""";
        extra += workLocation == null ? string.Empty : $@",""workLocation"":""{workLocation}""";

        return SaveRows(
            "S3.1",
            $@"{{""code"":""{code}"",""nameEn"":""Ahmed Ali"",""nameAr"":""أحمد علي"",""gender"":""Male"",
                ""birthDate"":""1984-01-27"",""nationality"":""Egyptian"",""civilId"":""284012703579"",
                ""mobile"":""99709947"",""joiningDate"":""2019-03-01"",""active"":""Yes""{extra}}}");
    }

    private static GoLiveApplyStageDto Stage(GoLiveApplyResultDto result, string target) =>
        result.Stages.Single(s => s.Target == target);

    private int AddJobTitle(string nameEn) => UsingDbContext(context =>
    {
        var jobTitle = new JobTitle
        {
            Code = nameEn.Substring(0, 3).ToUpperInvariant(),
            NameEn = nameEn,
            NameAr = nameEn,
            TenantId = AbpSession.TenantId ?? 1,
        };

        context.JobTitles.Add(jobTitle);
        context.SaveChanges();
        return jobTitle.Id;
    });

    // ---------------------------------------------------------------- config: payroll area

    [Fact]
    public async Task Should_Create_A_Payroll_Area_From_The_Company_Answers()
    {
        await SaveCompany();

        var result = await _apply.Apply();

        Stage(result, "Configuration").Created.ShouldBe(1);
        result.IsClean.ShouldBeTrue();

        UsingDbContext(context =>
        {
            var area = context.PayrollAreas.Single();
            area.Code.ShouldBe("MAIN");
            area.Name.ShouldBe("Gulf Business Solutions");
            area.CurrencyId.ShouldBeGreaterThan(0);
            area.PayrollPeriodId.ShouldBeGreaterThan(0);
        });
    }

    [Fact]
    public async Task Should_Take_The_Cut_Off_Day_From_The_Pay_Rules_Step()
    {
        await SaveCompany();
        await SaveAnswers("S1.2", @"""Q2.03"":25");

        await _apply.Apply();

        UsingDbContext(context => context.PayrollAreas.Single().CutOffDay.ShouldBe(25));
    }

    [Fact]
    public async Task Should_Default_The_Cut_Off_To_Month_End_When_Unanswered()
    {
        // Month end is the least surprising default: it includes everything entered that month.
        await SaveCompany();

        await _apply.Apply();

        UsingDbContext(context => context.PayrollAreas.Single().CutOffDay.ShouldBe(31));
    }

    [Fact]
    public async Task Should_Not_Invent_A_Payroll_Area_Without_A_Company_Name()
    {
        // A placeholder name would end up on every payslip.
        await SaveAnswers("S1.1", @"""Q1.04"":""Kuwaiti Dinar""");

        var result = await _apply.Apply();

        Stage(result, "Configuration").Created.ShouldBe(0);
        UsingDbContext(context => context.PayrollAreas.Count().ShouldBe(0));
    }

    [Fact]
    public async Task Should_Pick_The_Payroll_Period_Matching_The_Financial_Year()
    {
        await SaveCompany(fiscalStart: "April");

        await _apply.Apply();

        UsingDbContext(context =>
        {
            var area = context.PayrollAreas.Single();
            var period = context.PayrollPeriods.Single(p => p.Id == area.PayrollPeriodId);
            period.Code.ShouldBe("Apr-Mar");
        });
    }

    // ---------------------------------------------------------------- config: annual leave

    [Fact]
    public async Task Should_Create_The_Annual_Leave_Type_From_The_Leave_Answers()
    {
        await SaveAnswers("S1.5", @"""Q5.01"":30", @"""Q5.03"":""CalendarYear""");

        var result = await _apply.Apply();

        result.IsClean.ShouldBeTrue();

        UsingDbContext(context =>
        {
            var leave = context.AbsenceTypes.Single(a => a.Code == "ANNUAL");
            leave.NameEn.ShouldBe("Annual Leave");
            leave.IsPaid.ShouldBeTrue();
            leave.HaveQuotaAccrual.ShouldBeTrue();
            leave.AccrualPeriod.ShouldBe(AccrualPeriod.CalendarYear);
        });
    }

    [Fact]
    public async Task Should_Reset_Leave_On_The_Work_Anniversary_When_That_Is_The_Answer()
    {
        await SaveAnswers("S1.5", @"""Q5.01"":30", @"""Q5.03"":""WorkYear""");

        await _apply.Apply();

        UsingDbContext(context =>
            context.AbsenceTypes.Single(a => a.Code == "ANNUAL").AccrualPeriod.ShouldBe(AccrualPeriod.WorkYear));
    }

    [Fact]
    public async Task Should_Count_Weekly_Offs_As_Leave_Only_When_Asked_To()
    {
        await SaveAnswers("S1.5", @"""Q5.01"":30", @"""Q5.04"":""Yes""");

        await _apply.Apply();

        UsingDbContext(context =>
            context.AbsenceTypes.Single(a => a.Code == "ANNUAL").IncludeWeekend.ShouldBeTrue());
    }

    [Fact]
    public async Task Should_Not_Create_A_Leave_Type_Before_The_Days_Are_Answered()
    {
        await SaveCompany();

        await _apply.Apply();

        UsingDbContext(context => context.AbsenceTypes.Count(a => a.Code == "ANNUAL").ShouldBe(0));
    }

    // ---------------------------------------------------------------- config: reruns and preview

    [Fact]
    public async Task Should_Update_The_Payroll_Area_Rather_Than_Add_A_Second_One()
    {
        await SaveCompany();
        await _apply.Apply();

        await SaveCompany("Gulf Business Solutions WLL");
        var second = await _apply.Apply();

        Stage(second, "Configuration").Created.ShouldBe(0);
        Stage(second, "Configuration").Updated.ShouldBe(1);

        UsingDbContext(context =>
        {
            context.PayrollAreas.Count().ShouldBe(1);
            context.PayrollAreas.Single().Name.ShouldBe("Gulf Business Solutions WLL");
        });
    }

    [Fact]
    public async Task Should_Write_No_Configuration_On_Preview()
    {
        await SaveCompany();
        await SaveAnswers("S1.5", @"""Q5.01"":30");

        var result = await _apply.Preview();

        Stage(result, "Configuration").Created.ShouldBe(2);

        UsingDbContext(context =>
        {
            context.PayrollAreas.Count().ShouldBe(0);
            context.AbsenceTypes.Count(a => a.Code == "ANNUAL").ShouldBe(0);
        });
    }

    // ---------------------------------------------------------------- org assignment

    [Fact]
    public async Task Should_Attach_Every_Employee_To_The_Payroll_Area()
    {
        // Without this an employee exists, looks right on screen, and is silently left out of the run.
        await SaveCompany();
        await SaveEmployee("1001");

        var result = await _apply.Apply();

        Stage(result, "EmployeeOrgAssignment").Created.ShouldBe(1);
        result.IsClean.ShouldBeTrue();

        UsingDbContext(context =>
        {
            var assignment = context.EmployeeOrgAssignments.Single();
            var area = context.PayrollAreas.Single();
            assignment.PayrollAreaId.ShouldBe(area.Id);
        });
    }

    [Fact]
    public async Task Should_Date_The_Assignment_From_The_Joining_Date()
    {
        await SaveCompany();
        await SaveEmployee("1001");

        await _apply.Apply();

        UsingDbContext(context =>
        {
            var assignment = context.EmployeeOrgAssignments.Single();
            assignment.StartDate.Date.ShouldBe(new DateTime(2019, 3, 1));
            assignment.EndDate.Date.ShouldBe(new DateTime(9999, 12, 31));
        });
    }

    [Fact]
    public async Task Should_Resolve_The_Job_Title_By_Name()
    {
        var jobTitleId = AddJobTitle("Accountant");
        await SaveCompany();
        await SaveEmployee("1001", jobTitle: "Accountant");

        var result = await _apply.Apply();

        result.IsClean.ShouldBeTrue();
        UsingDbContext(context => context.EmployeeOrgAssignments.Single().JobTitleId.ShouldBe(jobTitleId));
    }

    [Fact]
    public async Task Should_Report_A_Job_Title_That_Is_Not_Set_Up()
    {
        await SaveCompany();
        await SaveEmployee("1001", jobTitle: "Chief Astronaut");

        var result = await _apply.Apply();

        var stage = Stage(result, "EmployeeOrgAssignment");
        stage.Failed.ShouldBe(1);
        stage.Issues.Single().Message.ShouldContain("Chief Astronaut");
        stage.Issues.Single().Key.ShouldBe("1001");
    }

    [Fact]
    public async Task Should_Leave_An_Unmatched_Work_Location_Unset_Rather_Than_Fail()
    {
        // Work location is optional on the sheet, so a typo should not keep somebody out of payroll.
        await SaveCompany();
        await SaveEmployee("1001", workLocation: "Somewhere Else");

        var result = await _apply.Apply();

        result.IsClean.ShouldBeTrue();
        UsingDbContext(context => context.EmployeeOrgAssignments.Single().WorkLocationId.ShouldBeNull());
    }

    [Fact]
    public async Task Should_Update_The_Assignment_Rather_Than_Duplicate_It()
    {
        AddJobTitle("Accountant");
        await SaveCompany();
        await SaveEmployee("1001");
        await _apply.Apply();

        await SaveEmployee("1001", jobTitle: "Accountant");
        var second = await _apply.Apply();

        Stage(second, "EmployeeOrgAssignment").Created.ShouldBe(0);
        Stage(second, "EmployeeOrgAssignment").Updated.ShouldBe(1);

        UsingDbContext(context =>
        {
            context.EmployeeOrgAssignments.Count().ShouldBe(1);
            context.EmployeeOrgAssignments.Single().JobTitleId.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Should_Write_No_Assignment_On_Preview()
    {
        await SaveCompany();
        await SaveEmployee("1001");

        var result = await _apply.Preview();

        Stage(result, "EmployeeOrgAssignment").Created.ShouldBe(1);
        UsingDbContext(context => context.EmployeeOrgAssignments.Count().ShouldBe(0));
    }

    [Fact]
    public async Task Should_Run_Configuration_Before_Anything_Points_At_It()
    {
        await SaveCompany();
        await SaveEmployee("1001");

        var result = await _apply.Apply();

        var targets = result.Stages.Select(s => s.Target).ToList();
        targets.IndexOf("Configuration").ShouldBeLessThan(targets.IndexOf("EmployeeOrgAssignment"));
        targets.IndexOf("Employee").ShouldBeLessThan(targets.IndexOf("EmployeeOrgAssignment"));
    }
}
