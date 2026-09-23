using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.SystemSecurity;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the six wage-account-driven Payroll JV (Custom 042) grid reports -- legacy
/// <c>uscRptPayrollJV_Custom042.ascx.cs</c>, <c>Table1()</c> branches "Salary JV" (lines 980-1350),
/// "AnnualLeave Provision JV" (1794-1925), "Indemnity Provision JV" (2793-2924), "Overtime JV"
/// (2927-3065), "Missing Accounts" (3068-3137) and "EmployeeWise BreakDown" (3140-3236). All six share
/// <see cref="PayrollJVPostingLineLoader"/> with the rest of the wage-account-driven JV family, so the
/// arrangement reuses <see cref="PayrollJVReportTestDataBuilder"/>.
///
/// The three quota-accrual Leave Provision variants (Public Holiday / Ramadan Time Back / Lieu Day) and
/// the per-employee-category Advance Salary JV variant are NOT covered here -- see the app-service remarks
/// for the additional entities (EmployeeAttendance/TimeRegister/EmployeeAbsAdjustment/
/// SalaryRegisterDaysDetail) their arrangement would require; they were exercised only by manual code
/// review in this porting wave.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom042ReportAppServices_Tests : AppTestBase
{
    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom042ReportAppServices_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    // ----------------------------------------------------------------------------------------
    // Salary JV
    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// Legacy lines 1023-1080: an Earning line posts Debit when positive, and the NetPay entry (lines
    /// 1305-1349) posts Credit when positive -- both formulas pinned together since they use OPPOSITE
    /// sign conventions for the same-signed amount.
    /// </summary>
    [Fact]
    public async Task SalaryJv_Should_Reproduce_The_Legacy_Debit_Credit_And_NetPay_Sign_Conventions()
    {
        var appService = Resolve<IPayrollJVCustom042SalaryReportAppService>();
        var earning = _data.CreateWageType("1000", BillingType.Earning);
        var netPay = _data.CreateWageType("9000", BillingType.Earning);
        var mainAccount = _data.CreateAccount("80001", "Salary Expense");
        var netAccount = _data.CreateAccount("81001", "Net Pay Payable");

        var employee = _data.CreateEmployee("JV42SAL-1");
        _data.CreateWageAccount(employee, earning, mainAccount);
        _data.CreateWageAccount(employee, netPay, netAccount);

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, earning, 500);
        _data.CreateDetail(register, netPay, 500);

        var result = await appService.GetPayrollJVCustom042SalaryReport(SalaryInput());

        result.Items.Count.ShouldBe(2);
        var earningRow = result.Items.Single(r => r.MainAccountCode == "80001");
        earningRow.DrCr.ShouldBe("D");
        earningRow.FcAmount.ShouldBe(500);

        var netRow = result.Items.Single(r => r.MainAccountCode == "81001");
        netRow.DrCr.ShouldBe("C");
        netRow.Description.ShouldContain("Being Net Salary Payable");
    }

    /// <summary>Legacy lines 1063-1065: an unmapped cost element is written as the literal string "NULL".</summary>
    [Fact]
    public async Task SalaryJv_Should_Write_The_Literal_String_NULL_For_An_Unmapped_Division()
    {
        var appService = Resolve<IPayrollJVCustom042SalaryReportAppService>();
        var earning = _data.CreateWageType("1001", BillingType.Earning);
        var account = _data.CreateAccount("80002", "Allowance Expense");

        var employee = _data.CreateEmployee("JV42SAL-2");
        _data.CreateWageAccount(employee, earning, account); // no cost elements

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), earning, 200);

        var result = await appService.GetPayrollJVCustom042SalaryReport(SalaryInput());

        result.Items.Single().Division.ShouldBe("NULL");
    }

    // ----------------------------------------------------------------------------------------
    // AnnualLeave Provision JV / Indemnity Provision JV
    // ----------------------------------------------------------------------------------------

    [Fact]
    public async Task AnnualLeaveProvisionJv_Should_Book_A_Debit_Line_Plus_A_Closing_Credit_Line()
    {
        var appService = Resolve<IPayrollJVCustom042AnnualLeaveProvisionReportAppService>();
        var provisionWageType = _data.CreateWageType("3001", BillingType.Provision);
        var account = _data.CreateAccount("61400", "AL Provision");
        var closingAccount = _data.CreateAccount("614000", "AL Provision Control");

        var employee = _data.CreateEmployee("JV42AL-1");
        _data.CreateWageAccount(employee, provisionWageType, account);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), provisionWageType, 300);

        var result = await appService.GetPayrollJVCustom042AnnualLeaveProvisionReport(
            new GetPayrollJVCustom042AnnualLeaveProvisionReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        result.Items.Count.ShouldBe(2);
        result.Items.Single(r => r.MainAccountCode == "61400").DrCr.ShouldBe("D");
        var closing = result.Items.Single(r => r.MainAccountCode == "614000");
        closing.DrCr.ShouldBe("C");
        closing.Description.ShouldContain("Provision-Total Annual Leave Provision");
    }

    [Fact]
    public async Task IndemnityProvisionJv_Should_Book_A_Debit_Line_Plus_A_Closing_Credit_Line()
    {
        var appService = Resolve<IPayrollJVCustom042IndemnityProvisionReportAppService>();
        var provisionWageType = _data.CreateWageType("3000", BillingType.Provision);
        var account = _data.CreateAccount("61300", "Indemnity Provision");
        var closingAccount = _data.CreateAccount("613000", "Indemnity Provision Control");

        var employee = _data.CreateEmployee("JV42IND-1");
        _data.CreateWageAccount(employee, provisionWageType, account);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), provisionWageType, 150);

        var result = await appService.GetPayrollJVCustom042IndemnityProvisionReport(
            new GetPayrollJVCustom042IndemnityProvisionReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        result.Items.Count.ShouldBe(2);
        var closing = result.Items.Single(r => r.MainAccountCode == "613000");
        closing.Description.ShouldContain("Provision-Total Indemnity Provision");
    }

    // ----------------------------------------------------------------------------------------
    // Overtime JV
    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// Legacy lines 3002-3004: unlike the other formats, Overtime JV writes EMPTY STRING (not "NULL") for
    /// an unmapped cost element.
    /// </summary>
    [Fact]
    public async Task OvertimeJv_Should_Write_Empty_String_Not_NULL_For_An_Unmapped_Division()
    {
        var appService = Resolve<IPayrollJVCustom042OvertimeReportAppService>();
        var overtime = _data.CreateWageType("1007", BillingType.Earning);
        var account = _data.CreateAccount("80500", "Overtime Expense");
        var otControlAccount = _data.CreateAccount("516000", "Overtime Control");

        var employee = _data.CreateEmployee("JV42OT-1");
        var empAccount = _data.CreateAccount(employee.Code, "Employee Account");
        _data.CreateWageAccount(employee, overtime, account);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), overtime, 80);

        var result = await appService.GetPayrollJVCustom042OvertimeReport(
            new GetPayrollJVCustom042OvertimeReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        result.Items.Count.ShouldBe(2);
        var divisionEntry = result.Items.Single(r => r.MainAccountCode == "80500");
        divisionEntry.Division.ShouldBe(string.Empty);

        var employeeEntry = result.Items.Single(r => r.MainAccountCode == "516000");
        employeeEntry.SubAccountCode.ShouldBe(employee.Code);
        employeeEntry.DrCr.ShouldBe("C");
    }

    // ----------------------------------------------------------------------------------------
    // Missing Accounts
    // ----------------------------------------------------------------------------------------

    [Fact]
    public async Task MissingAccounts_Should_List_One_Row_Per_Raw_Line_With_No_Grouping()
    {
        var appService = Resolve<IPayrollJVCustom042MissingAccountsReportAppService>();
        var wageType = _data.CreateWageType("1010", BillingType.Earning);

        var employee = _data.CreateEmployee("JV42MA-1");
        // No CreateWageAccount call: this wage type has no mapping for this employee.
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 50);

        var result = await appService.GetPayrollJVCustom042MissingAccountsReport(
            new GetPayrollJVCustom042MissingAccountsReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        result.Items.Count.ShouldBe(1);
        result.Items.Single().EmployeeCode.ShouldBe(employee.Code);
        result.Items.Single().Amount.ShouldBe(50);
    }

    // ----------------------------------------------------------------------------------------
    // EmployeeWise BreakDown
    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// Legacy lines 3228-3229: NOT Math.Abs -- a negative net amount (e.g. a deduction line) is written
    /// as ZERO, not its absolute value, even though the line survives the "!= 0" filter and appears with
    /// its correct Debit/Credit flag.
    /// </summary>
    [Fact]
    public async Task EmployeeWiseBreakDown_Should_Reproduce_The_Legacy_Zeroed_Negative_Amount_Defect()
    {
        var appService = Resolve<IPayrollJVCustom042EmployeeWiseBreakDownReportAppService>();
        var deduction = _data.CreateWageType("2001", BillingType.Deduction);
        var account = _data.CreateAccount("80700", "Deduction Payable");

        var employee = _data.CreateEmployee("JV42EW-1");
        _data.CreateWageAccount(employee, deduction, account);
        // A negative stored Amount (e.g. a corrective line) is what legacy's own Fc-amount/Net-amount
        // formula (`rec.CostAmount >= 0 ? rec.CostAmount : 0`) zeroes, unlike every other Custom 042
        // format's Math.Abs.
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), deduction, -75);

        var result = await appService.GetPayrollJVCustom042EmployeeWiseBreakDownReport(
            new GetPayrollJVCustom042EmployeeWiseBreakDownReportInput { Month = 6, Year = 2024, MaxResultCount = 100 });

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe(employee.Code);
        row.DrCr.ShouldBe("C");
        row.FcAmount.ShouldBe(0);
    }

    // ----------------------------------------------------------------------------------------
    // ENABLE_WAGEACCOUNT_BY_ORGUNIT (legacy lines 681-830)
    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// Legacy lines 681-760: with the tenant config key on, the wage accounts come from the group mapped to
    /// the employee's organization unit, and an <see cref="OffsetAccountInfoType.EmployeeCode"/> row's
    /// offset is the account whose code is the employee's own code. No <c>EmployeeWageAccount</c> row is
    /// needed at all.
    /// </summary>
    [Fact]
    public async Task SalaryJv_Should_Take_Wage_Accounts_From_The_Org_Unit_Group_When_The_Tenant_Config_Key_Is_On()
    {
        var appService = Resolve<IPayrollJVCustom042SalaryReportAppService>();
        _data.CreateTenantConfig(SysTenantConfigKeys.EnableWageAccountByOrgUnit, "1");

        var earning = _data.CreateWageType("1002", BillingType.Earning);
        var groupAccount = _data.CreateAccount("80003", "Group Salary Expense");
        var division = _data.CreateCostElement("DIV-ORG");
        var employee = _data.CreateEmployee("JV42ORG-1");
        _data.CreateAccount("JV42ORG-1", "Employee sub-ledger");

        var unit = _data.CreateOrganizationUnit("Org-unit wage accounts");
        var group = _data.CreateWageAccGroup("WAG-1");
        _data.CreateWageAccGroupDetail(group, earning, groupAccount, OffsetAccountInfoType.EmployeeCode, costElement1: division);
        _data.MapOrganizationUnitToWageAccGroup(unit, group);
        _data.CreateOrgAssignment(employee, null, new DateTime(2000, 1, 1), new DateTime(2099, 12, 31), organizationUnit: unit);

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), earning, 400);

        var result = await appService.GetPayrollJVCustom042SalaryReport(SalaryInput());

        var row = result.Items.Single();
        row.MainAccountCode.ShouldBe("80003");
        row.SubAccountCode.ShouldBe("JV42ORG-1");
        row.Division.ShouldBe("DIV-ORG");
        row.FcAmount.ShouldBe(400);
    }

    /// <summary>
    /// Legacy line 732-736: a <see cref="OffsetAccountInfoType.Fixed"/> group row keeps its own offset
    /// account, and a mapping that has expired by the month end is ignored (line 750).
    /// </summary>
    [Fact]
    public async Task SalaryJv_Should_Use_The_Fixed_Offset_And_Only_Mappings_Current_At_Month_End()
    {
        var appService = Resolve<IPayrollJVCustom042SalaryReportAppService>();
        _data.CreateTenantConfig(SysTenantConfigKeys.EnableWageAccountByOrgUnit, "1");

        var earning = _data.CreateWageType("1003", BillingType.Earning);
        var currentAccount = _data.CreateAccount("80004", "Current group account");
        var expiredAccount = _data.CreateAccount("80005", "Expired group account");
        var fixedOffset = _data.CreateAccount("21000", "Salaries payable");
        var employee = _data.CreateEmployee("JV42ORG-2");

        var unit = _data.CreateOrganizationUnit("Org-unit with two mappings");
        var currentGroup = _data.CreateWageAccGroup("WAG-2");
        _data.CreateWageAccGroupDetail(currentGroup, earning, currentAccount, OffsetAccountInfoType.Fixed, fixedOffset);
        _data.MapOrganizationUnitToWageAccGroup(unit, currentGroup);

        var expiredGroup = _data.CreateWageAccGroup("WAG-3");
        _data.CreateWageAccGroupDetail(expiredGroup, earning, expiredAccount);
        _data.MapOrganizationUnitToWageAccGroup(unit, expiredGroup, new DateTime(2000, 1, 1), new DateTime(2024, 5, 31));

        _data.CreateOrgAssignment(employee, null, new DateTime(2000, 1, 1), new DateTime(2099, 12, 31), organizationUnit: unit);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), earning, 250);

        var result = await appService.GetPayrollJVCustom042SalaryReport(SalaryInput());

        var row = result.Items.Single();
        row.MainAccountCode.ShouldBe("80004");
        row.SubAccountCode.ShouldBe("21000");
    }

    /// <summary>Legacy lines 768-771: with the key off (or absent) the org-unit group is never consulted.</summary>
    [Fact]
    public async Task SalaryJv_Should_Ignore_The_Org_Unit_Group_When_The_Tenant_Config_Key_Is_Off()
    {
        var appService = Resolve<IPayrollJVCustom042SalaryReportAppService>();
        _data.CreateTenantConfig(SysTenantConfigKeys.EnableWageAccountByOrgUnit, "0");

        var earning = _data.CreateWageType("1004", BillingType.Earning);
        var groupAccount = _data.CreateAccount("80006", "Group account");
        var ownAccount = _data.CreateAccount("80007", "Employee's own account");
        var employee = _data.CreateEmployee("JV42ORG-3");

        var unit = _data.CreateOrganizationUnit("Org-unit ignored");
        var group = _data.CreateWageAccGroup("WAG-4");
        _data.CreateWageAccGroupDetail(group, earning, groupAccount);
        _data.MapOrganizationUnitToWageAccGroup(unit, group);
        _data.CreateOrgAssignment(employee, null, new DateTime(2000, 1, 1), new DateTime(2099, 12, 31), organizationUnit: unit);
        _data.CreateWageAccount(employee, earning, ownAccount);

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), earning, 100);

        var result = await appService.GetPayrollJVCustom042SalaryReport(SalaryInput());

        result.Items.Single().MainAccountCode.ShouldBe("80007");
    }

    private static GetPayrollJVCustom042SalaryReportInput SalaryInput(int maxResultCount = 100)
    {
        return new GetPayrollJVCustom042SalaryReportInput { Month = 6, Year = 2024, MaxResultCount = maxResultCount };
    }
}
