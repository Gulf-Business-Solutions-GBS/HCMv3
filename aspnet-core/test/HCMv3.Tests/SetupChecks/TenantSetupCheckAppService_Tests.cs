using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.Releases;
using HCMv3.SetupChecks;
using HCMv3.SetupChecks.Dto;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SetupChecks;

// ReSharper disable once InconsistentNaming
public class TenantSetupCheckAppService_Tests : TenantSetupCheckTestBase
{
    private readonly ITenantSetupCheckAppService _appService;

    public TenantSetupCheckAppService_Tests()
    {
        LoginAsHostAdmin();
        _appService = Resolve<ITenantSetupCheckAppService>();
    }

    private Task<TenantSetupCheckReportDto> Check(int tenantId, string version = HCMv3Releases.V3_0_0, bool onlyThisVersion = false) =>
        _appService.RunChecks(new RunTenantSetupChecksInput { TenantId = tenantId, Version = version, OnlyThisVersion = onlyThisVersion });

    private static TenantSetupCheckItemDto Item(TenantSetupCheckReportDto report, string code) => report.Items.Single(i => i.Code == code);

    // ---------------------------------------------------------------- options

    [Fact]
    public async Task Should_Offer_The_Releases_Newest_First_With_Their_Check_Counts()
    {
        var options = await _appService.GetOptions();

        options.CurrentVersion.ShouldBe(HCMv3Releases.Current.Version);
        options.Releases.Select(r => r.Version).ShouldBe(HCMv3Releases.All.Reverse().Select(r => r.Version));
        var baseline = options.Releases.Single(r => r.Version == HCMv3Releases.V3_0_0);
        baseline.CheckCount.ShouldBe(15);
        baseline.Title.ShouldBe("HCMv3 3.0.0 - Baseline");
    }

    [Fact]
    public async Task Should_Offer_Every_Tenant()
    {
        var tenantId = CreateEmptyTenant("setupoptions");

        var options = await _appService.GetOptions();

        options.Tenants.ShouldContain(t => t.Id == tenantId && t.TenancyName == "setupoptions" && t.Name == "setupoptions Co");
    }

    // ---------------------------------------------------------------- whole tenant

    [Fact]
    public async Task Should_Report_Every_Check_In_Place_For_A_Fully_Set_Up_Tenant()
    {
        var tenantId = CreateEmptyTenant("complete");
        var areaId = CreateCompletePayrollSetup(tenantId);
        CreateEmployee(tenantId, "E001", areaId);

        var report = await Check(tenantId);

        report.TenantId.ShouldBe(tenantId);
        report.TenantName.ShouldBe("complete Co");
        report.Items.Count.ShouldBe(15);
        report.Items.Where(i => i.Outcome != SetupCheckOutcome.Ok).Select(i => i.Code).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Report_Payroll_Missing_And_Employee_Checks_Not_Applicable_For_An_Empty_Tenant()
    {
        var tenantId = CreateEmptyTenant("empty");

        var report = await Check(tenantId);

        Item(report, "PAY-001").Outcome.ShouldBe(SetupCheckOutcome.Missing);
        Item(report, "PAY-002").Outcome.ShouldBe(SetupCheckOutcome.NotApplicable);
        Item(report, "PAY-003").Outcome.ShouldBe(SetupCheckOutcome.NotApplicable);
        Item(report, "PAY-005").Outcome.ShouldBe(SetupCheckOutcome.Missing);
        report.Items.Where(i => i.Code.StartsWith("EMP-")).ShouldAllBe(i => i.Outcome == SetupCheckOutcome.NotApplicable);
    }

    [Fact]
    public async Task Should_Name_The_Settings_Screens_Still_To_Be_Saved()
    {
        var tenantId = CreateEmptyTenant("settings");
        CreateSettingsRows(tenantId, time: true, payroll: false, personnel: true, security: false);

        var item = Item(await Check(tenantId), "PAY-004");

        item.Outcome.ShouldBe(SetupCheckOutcome.Missing);
        item.CheckedCount.ShouldBe(4);
        item.Examples.ShouldBe(new[] { "SST007", "SST011" });
    }

    [Fact]
    public async Task Should_Name_The_Payroll_Areas_Without_A_Control_Record_Or_Calendar()
    {
        var tenantId = CreateEmptyTenant("areas");
        CreateCompletePayrollSetup(tenantId, "MAIN");
        CreatePayrollArea(tenantId, "SECOND");

        var report = await Check(tenantId);

        foreach (var code in new[] { "PAY-002", "PAY-003" })
        {
            var item = Item(report, code);
            item.Outcome.ShouldBe(SetupCheckOutcome.Missing);
            item.CheckedCount.ShouldBe(2);
            item.Examples.ShouldBe(new[] { "SECOND" });
        }
    }

    // ---------------------------------------------------------------- one missing piece at a time

    [Theory]
    [InlineData(TenantSetupCheckTestBase_Parts.Contract, new[] { "EMP-001" })]
    [InlineData(TenantSetupCheckTestBase_Parts.OrgAssignment, new[] { "EMP-002", "EMP-003", "EMP-004" })]
    [InlineData(TenantSetupCheckTestBase_Parts.PayrollAreaOnAssignment, new[] { "EMP-003" })]
    [InlineData(TenantSetupCheckTestBase_Parts.GroupAndSubGroup, new[] { "EMP-004" })]
    [InlineData(TenantSetupCheckTestBase_Parts.SalaryRecord, new[] { "EMP-005" })]
    [InlineData(TenantSetupCheckTestBase_Parts.PayrollStatus, new[] { "EMP-006" })]
    [InlineData(TenantSetupCheckTestBase_Parts.DutyPeriod, new[] { "EMP-007" })]
    [InlineData(TenantSetupCheckTestBase_Parts.TimePolicy, new[] { "EMP-008" })]
    [InlineData(TenantSetupCheckTestBase_Parts.BankWithIban, new[] { "EMP-009" })]
    public async Task Should_Report_Exactly_The_Checks_A_Missing_Employee_Record_Fails(int leftOut, string[] expectedMissing)
    {
        var tenantId = CreateEmptyTenant("oneoff");
        var areaId = CreateCompletePayrollSetup(tenantId);
        CreateEmployee(tenantId, "E001", areaId);
        CreateEmployee(tenantId, "E002", areaId, EmployeeParts.All & ~(EmployeeParts)leftOut);

        var report = await Check(tenantId);

        report.Items.Where(i => i.Outcome == SetupCheckOutcome.Missing).Select(i => i.Code).ShouldBe(expectedMissing);
        foreach (var code in expectedMissing)
        {
            var item = Item(report, code);
            item.CheckedCount.ShouldBe(2);
            item.MissingCount.ShouldBe(1);
            item.Examples.ShouldBe(new[] { "E002" });
        }
    }

    [Fact]
    public async Task Should_Find_The_Payroll_Area_Through_The_Organization_Unit_When_The_Assignment_Has_None()
    {
        var tenantId = CreateEmptyTenant("orgunit");
        var areaId = CreateCompletePayrollSetup(tenantId);
        var unitId = CreateOrgUnitWithPayrollArea(tenantId, areaId);
        CreateEmployee(tenantId, "E001", areaId, EmployeeParts.All & ~EmployeeParts.PayrollAreaOnAssignment, organizationUnitId: unitId);

        Item(await Check(tenantId), "EMP-003").Outcome.ShouldBe(SetupCheckOutcome.Ok);
    }

    [Fact]
    public async Task Should_Ignore_Inactive_Employees_And_Those_Not_Yet_Joined()
    {
        var tenantId = CreateEmptyTenant("inactive");
        var areaId = CreateCompletePayrollSetup(tenantId);
        CreateEmployee(tenantId, "E001", areaId);
        CreateEmployee(tenantId, "LEFT", areaId, EmployeeParts.None, status: EmployeeStatus.Inactive);
        CreateEmployee(tenantId, "NEXT", areaId, EmployeeParts.None, joiningDate: DateTime.Today.AddYears(1));

        var report = await Check(tenantId);

        report.Items.Where(i => i.Outcome != SetupCheckOutcome.Ok).Select(i => i.Code).ShouldBeEmpty();
        Item(report, "EMP-001").CheckedCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Check_Accounts_Only_For_Employees_Paid_By_Bank()
    {
        var tenantId = CreateEmptyTenant("banks");
        var areaId = CreateCompletePayrollSetup(tenantId);
        var noAccount = CreateEmployee(tenantId, "NOACC", areaId, EmployeeParts.All & ~EmployeeParts.BankWithIban);
        AddPaymentDetails(tenantId, noAccount, PaymentType.Bank, iban: " ", accountNumber: null);
        var accountOnly = CreateEmployee(tenantId, "ACCONLY", areaId, EmployeeParts.All & ~EmployeeParts.BankWithIban);
        AddPaymentDetails(tenantId, accountOnly, PaymentType.Bank, iban: null, accountNumber: "123");
        CreateEmployee(tenantId, "CASH", areaId, paymentType: PaymentType.Cash);

        var item = Item(await Check(tenantId), "EMP-010");

        item.Outcome.ShouldBe(SetupCheckOutcome.Missing);
        item.CheckedCount.ShouldBe(2);
        item.Examples.ShouldBe(new[] { "NOACC" });
    }

    [Fact]
    public async Task Should_Show_At_Most_Twenty_Codes_But_Count_Them_All()
    {
        var tenantId = CreateEmptyTenant("many");
        var areaId = CreateCompletePayrollSetup(tenantId);
        for (var i = 1; i <= 25; i++)
        {
            CreateEmployee(tenantId, $"E{i:000}", areaId, EmployeeParts.All & ~EmployeeParts.SalaryRecord);
        }

        var item = Item(await Check(tenantId), "EMP-005");

        item.MissingCount.ShouldBe(25);
        item.Examples.Count.ShouldBe(SetupCheckFinding.MaxExamples);
        item.Examples.First().ShouldBe("E001");
    }

    // ---------------------------------------------------------------- tenant isolation

    [Fact]
    public async Task Should_Never_Count_Another_Tenants_Setup()
    {
        var complete = CreateEmptyTenant("tenanta");
        var areaId = CreateCompletePayrollSetup(complete);
        CreateEmployee(complete, "A001", areaId);
        var empty = CreateEmptyTenant("tenantb");
        CreateEmployee(empty, "B001", 0, EmployeeParts.None);

        var emptyReport = await Check(empty);
        var completeReport = await Check(complete);

        Item(emptyReport, "PAY-001").Outcome.ShouldBe(SetupCheckOutcome.Missing);
        Item(emptyReport, "EMP-001").Examples.ShouldBe(new[] { "B001" });
        emptyReport.Items.SelectMany(i => i.Examples).ShouldNotContain("A001");
        completeReport.Items.Where(i => i.Outcome != SetupCheckOutcome.Ok).Select(i => i.Code).ShouldBeEmpty();
        Item(completeReport, "EMP-001").CheckedCount.ShouldBe(1);
    }

    // ---------------------------------------------------------------- version choice and refusals

    [Fact]
    public async Task Should_Show_The_Same_Checks_Only_For_And_Up_To_The_First_Release()
    {
        var tenantId = CreateEmptyTenant("versions");

        var only = await Check(tenantId, onlyThisVersion: true);
        var upTo = await Check(tenantId, onlyThisVersion: false);

        only.Items.Select(i => i.Code).ShouldBe(upTo.Items.Select(i => i.Code));
        only.Items.ShouldAllBe(i => i.IntroducedIn == HCMv3Releases.V3_0_0);
        only.OnlyThisVersion.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_A_Version_Not_In_The_Release_List()
    {
        var tenantId = CreateEmptyTenant("badversion");

        await Should.ThrowAsync<UserFriendlyException>(() => Check(tenantId, "2.9.0"));
    }

    [Fact]
    public async Task Should_Refuse_A_Tenant_That_Does_Not_Exist()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => Check(987654));
    }

    [Fact]
    public async Task Should_Not_Be_Available_To_Tenant_Users()
    {
        var tenantId = CreateEmptyTenant("notforyou");
        LoginAsDefaultTenantAdmin();

        await Should.ThrowAsync<AbpAuthorizationException>(() => Check(tenantId));
    }

    // ---------------------------------------------------------------- texts and export

    [Fact]
    public async Task Should_Give_Each_Check_Its_Texts_And_Fix_Screen()
    {
        var tenantId = CreateEmptyTenant("texts");

        var item = Item(await Check(tenantId), "PAY-002");

        item.Title.ShouldBe("Every payroll area has a payroll control record");
        item.Why.ShouldContain("Payroll Control Record not found");
        item.ModuleName.ShouldBe("Payroll");
        item.Severity.ShouldBe(SetupCheckSeverity.Blocker);
        item.FixScreenCode.ShouldBe("SPY027");
        item.FixScreenName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Export_The_Report_As_An_Excel_File()
    {
        var tenantId = CreateEmptyTenant("export");

        var file = await _appService.ExportToExcel(new RunTenantSetupChecksInput { TenantId = tenantId, Version = HCMv3Releases.V3_0_0 });

        file.FileName.ShouldEndWith(".xlsx");
        file.FileName.ShouldContain("export Co");
        Resolve<ITempFileCacheManager>().GetFile(file.FileToken).ShouldNotBeEmpty();
    }
}

/// <summary>The fixture's parts as plain ints, which xUnit inline data can carry.</summary>
internal static class TenantSetupCheckTestBase_Parts
{
    public const int Contract = 1;
    public const int OrgAssignment = 2;
    public const int PayrollAreaOnAssignment = 4;
    public const int GroupAndSubGroup = 8;
    public const int SalaryRecord = 16;
    public const int PayrollStatus = 32;
    public const int DutyPeriod = 64;
    public const int TimePolicy = 128;
    public const int BankWithIban = 256;
}
