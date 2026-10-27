using System;
using System.Collections.Generic;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

/// <summary>
/// The key-to-setting mapping legacy <c>SysService.Get*Settings</c> applied when a tenant had no settings
/// row yet. The mappings worth pinning are the ones a plain "== 1" reading would get wrong: the two flags
/// that default to ON, the report-group names that stand for a client id, the dd/MM/yyyy date, and the
/// values legacy only assigned when the row existed.
/// </summary>
// ReSharper disable once InconsistentNaming
public class SysSettingsTenantConfigFactory_Tests
{
    private static SysTenantConfigLookup Config(params (string Key, string Value)[] rows)
    {
        var values = new Dictionary<string, string>();
        foreach (var (key, value) in rows)
        {
            values[key] = value;
        }

        return new SysTenantConfigLookup(values);
    }

    [Fact]
    public void Should_Default_The_Two_Legacy_On_Flags_To_True_When_The_Key_Is_Missing()
    {
        var payroll = SysSettingsTenantConfigFactory.CreatePayrollSettings(SysTenantConfigLookup.Empty);
        var time = SysSettingsTenantConfigFactory.CreateTimeSettings(SysTenantConfigLookup.Empty);

        payroll.EnableBasicpayMoreThenFixedDays.ShouldBeTrue();
        time.EnableOvertimeInTimeEngine.ShouldBeTrue();
    }

    [Fact]
    public void Should_Turn_The_Two_Legacy_On_Flags_Off_Only_For_An_Explicit_Zero()
    {
        var config = Config(
            (SysTenantConfigKeys.EnableBasicPayMoreThenFixedDays, "0"),
            (SysTenantConfigKeys.EnableOvertimeInTimeEngine, "0"));

        SysSettingsTenantConfigFactory.CreatePayrollSettings(config).EnableBasicpayMoreThenFixedDays.ShouldBeFalse();
        SysSettingsTenantConfigFactory.CreateTimeSettings(config).EnableOvertimeInTimeEngine.ShouldBeFalse();
    }

    [Fact]
    public void Should_Read_Every_Other_Flag_As_On_Only_For_One()
    {
        var on = Config((SysTenantConfigKeys.EnableRowLevelSecurity, "1"));
        var other = Config((SysTenantConfigKeys.EnableRowLevelSecurity, "true"));

        SysSettingsTenantConfigFactory.CreateSecuritySettings(on).EnableRowLevelSecurity.ShouldBeTrue();
        SysSettingsTenantConfigFactory.CreateSecuritySettings(other).EnableRowLevelSecurity.ShouldBeFalse();
        SysSettingsTenantConfigFactory.CreateSecuritySettings(SysTenantConfigLookup.Empty).EnableRowLevelSecurity.ShouldBeFalse();
    }

    [Theory]
    [InlineData("IMILL", "004")]
    [InlineData("LND", "007")]
    [InlineData("MASHAAN", "008")]
    [InlineData("CUSTOM19", "019")]
    [InlineData("019", "019")]
    [InlineData("", "")]
    public void Should_Map_Report_Group_Names_To_Client_Ids(string reportGroup, string expectedClientId)
    {
        var config = Config((SysTenantConfigKeys.ReportGroup, reportGroup));

        SysSettingsTenantConfigFactory.CreatePayrollSettings(config).ClientId.ShouldBe(expectedClientId);
    }

    [Fact]
    public void Should_Parse_Working_Hours_And_Bank_File_Cutoff()
    {
        var config = Config(
            (SysTenantConfigKeys.DefaultWorkingHours, "8.5"),
            (SysTenantConfigKeys.OffCycleBankFileCutoff, "20"));

        var payroll = SysSettingsTenantConfigFactory.CreatePayrollSettings(config);

        payroll.DefaultWorkingHours.ShouldBe(8.5);
        payroll.OffcycleBankFileCutoff.ShouldBe(20);
    }

    [Fact]
    public void Should_Leave_A_Number_At_Its_Default_When_The_Value_Is_Not_Numeric()
    {
        var config = Config((SysTenantConfigKeys.DefaultWorkingHours, "nine"));

        SysSettingsTenantConfigFactory.CreatePayrollSettings(config).DefaultWorkingHours.ShouldBe(0);
    }

    [Fact]
    public void Should_Parse_The_Earliest_Evaluation_Date_As_Day_Month_Year()
    {
        var config = Config(
            (SysTenantConfigKeys.TimeEvalEarliestDate, "01/04/2021"),
            (SysTenantConfigKeys.TeMaxLimitDays, "300"));

        var time = SysSettingsTenantConfigFactory.CreateTimeSettings(config);

        time.EarliestEvaluationDate.ShouldBe(new DateTime(2021, 4, 1));
        time.FutureEvaluationInDays.ShouldBe(300);
    }

    [Fact]
    public void Should_Leave_The_Earliest_Evaluation_Date_Unset_When_The_Key_Is_Blank()
    {
        var config = Config((SysTenantConfigKeys.TimeEvalEarliestDate, ""));

        SysSettingsTenantConfigFactory.CreateTimeSettings(config).EarliestEvaluationDate.ShouldBe(default);
    }

    [Fact]
    public void Should_Map_The_Renamed_Time_Keys_To_Their_Settings()
    {
        var config = Config(
            (SysTenantConfigKeys.EnableAttendanceDataDumping, "1"),
            (SysTenantConfigKeys.AutoAttendanceEnable, "1"),
            (SysTenantConfigKeys.ShowHideIsTravelling, "1"),
            (SysTenantConfigKeys.LeaveDeductionStartMonth, "6"));

        var time = SysSettingsTenantConfigFactory.CreateTimeSettings(config);

        time.EnableMiddleTierTimeEvents.ShouldBeTrue();
        time.EnableAttendanceOnHoliday.ShouldBeTrue();
        time.EnableIsTravelling.ShouldBeTrue();
        time.LeaveDeductionStartMonth.ShouldBe(6);
    }

    [Theory]
    [InlineData("QUARTERLY", AppraisalCycle.Quarterly)]
    [InlineData("halfyearly", AppraisalCycle.HalfYearly)]
    [InlineData("Annually", AppraisalCycle.Annually)]
    [InlineData("", AppraisalCycle.Quarterly)]
    public void Should_Map_The_Appraisal_Cycle_Name_Ignoring_Case(string value, AppraisalCycle expected)
    {
        var config = Config((SysTenantConfigKeys.AppraisalCycle, value));

        SysSettingsTenantConfigFactory.CreateAppraisalSettings(config).AppraisalCycle.ShouldBe(expected);
    }

    [Fact]
    public void Should_Map_Peer_Review_Limits_And_Workflow_Steps()
    {
        var config = Config(
            (SysTenantConfigKeys.EnablePaPeerReview, "1"),
            (SysTenantConfigKeys.SetPeerMinLimit, "2"),
            (SysTenantConfigKeys.SetPeerMaxLimit, "5"),
            (SysTenantConfigKeys.EnableUserWiseWorkflow, "1"),
            (SysTenantConfigKeys.EnableTravelCostOnWorkflowStep, "3"),
            (SysTenantConfigKeys.OvertimeRequestModificationStep, "2"));

        var appraisal = SysSettingsTenantConfigFactory.CreateAppraisalSettings(config);
        var workflow = SysSettingsTenantConfigFactory.CreateWorkflowSettings(config);

        appraisal.EnablePAPeerReview.ShouldBeTrue();
        appraisal.SetPeerMinLimit.ShouldBe(2);
        appraisal.SetPeerMaxLimit.ShouldBe(5);
        workflow.EnableWorkflowGroup.ShouldBeTrue();
        workflow.EnableTravelCostOnWorkflowstep.ShouldBe(3);
        workflow.OvertimeRequestModificationStep.ShouldBe(2);
    }

    [Fact]
    public void Should_Copy_Formatting_Strings_As_They_Are()
    {
        var config = Config(
            (SysTenantConfigKeys.DateEditMask, "dd/MM/yyyy"),
            (SysTenantConfigKeys.TimeDisplayFormat, "{0:HH\\:mm}"),
            (SysTenantConfigKeys.StartupDashboard, "Leave_Dashboard"));

        var general = SysSettingsTenantConfigFactory.CreateGeneralSettings(config);

        general.DateEditMask.ShouldBe("dd/MM/yyyy");
        general.TimeDisplayFormat.ShouldBe("{0:HH\\:mm}");
        general.StartupDashboard.ShouldBe("Leave_Dashboard");
        general.TimeEditMask.ShouldBe(string.Empty);
    }

    [Fact]
    public void Should_Read_The_Payslip_From_Date()
    {
        var config = Config(
            (SysTenantConfigKeys.ShowPayslipFromDate, "15/03/2023"),
            (SysTenantConfigKeys.ShowPayslipOnDay, "5"));

        var report = SysSettingsTenantConfigFactory.CreateReportSettings(config);

        report.ShowPayslipFromDate.ShouldBe(new DateTime(2023, 3, 15));
        report.ShowPayslipOnDay.ShouldBe(5);
    }

    [Fact]
    public void Should_Treat_A_Null_Value_As_Blank()
    {
        var config = Config((SysTenantConfigKeys.FileSystemAttachmentPath, null));

        config.Has(SysTenantConfigKeys.FileSystemAttachmentPath).ShouldBeTrue();
        SysSettingsTenantConfigFactory.CreateDocumentSettings(config).FileSystemAttachmentPath.ShouldBe(string.Empty);
    }
}
