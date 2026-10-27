using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Payroll JV (Custom 016) "Main Format-New" (MYB) grid report (legacy
/// <c>uscRptPayrollJV_Custom016.ascx</c>, <c>Table1()</c>, payroll-area-Name-contains-"MYB" column set,
/// <c>IsDetail == false</c> -- lines 573-1168 common preprocessing + 1578-2016).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom016MainFormatMybReportAppService_Tests : AppTestBase
{
    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private readonly IPayrollJVCustom016MainFormatMybReportAppService _appService;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom016MainFormatMybReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom016MainFormatMybReportAppService>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static GetPayrollJVCustom016MainFormatReportInput Input(
        int month = 6, int year = 2024, string employeeCode = null, PayrollRunType? runType = null,
        int maxResultCount = 100, int skipCount = 0)
    {
        return new GetPayrollJVCustom016MainFormatReportInput
        {
            Month = month,
            Year = year,
            EmployeeCode = employeeCode,
            PayrollRunType = runType,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    private Employee ArrangeLine(
        string employeeCode, string costCenterCode, string wageTypeCode, double amount,
        BillingType billingType, string accountCode, string offsetAccountCode, string mybAreaName = "MYB")
    {
        var mybArea = _data.CreatePayrollArea("MYB-" + employeeCode, mybAreaName);
        var costCenter = _data.CreateCostCenter(costCenterCode);
        var employee = _data.CreateEmployee(employeeCode);
        _data.CreateOrgAssignment(employee, mybArea, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), costCenter);

        var wageType = _data.CreateWageType(wageTypeCode, billingType);
        var account = _data.CreateAccount(accountCode, "Account " + accountCode);
        var offsetAccount = _data.CreateAccount(offsetAccountCode, "Offset " + offsetAccountCode);
        _data.CreateWageAccount(employee, wageType, account, offsetAccount, costPercentage: 100);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, amount);

        return employee;
    }

    /// <summary>
    /// Legacy lines 1959-2016: a non-special wage type produces a Debit-column row keyed by the REAL cost
    /// center (unlike the summary rows, which are forced to the literal "0000" bucket).
    /// </summary>
    [Fact]
    public async Task Should_Produce_CostCenter_Wise_Debit_Row_With_Real_Cost_Center()
    {
        ArrangeLine("JV16MFM-1", "1400", "2000", 300, BillingType.Earning, "90001", "90002");

        var result = await _appService.GetPayrollJVCustom016MainFormatMybReport(Input());

        var row = result.Items.Single(r => r.Exp == "090001");
        row.CostCenter.ShouldBe("1400");
        row.Debit.ShouldBe(300);
        row.Credit.ShouldBe(0);
    }

    /// <summary>Legacy lines 1722-1723: Leave Provision routes by sign WITHOUT pre-negation.</summary>
    [Fact]
    public async Task Should_Route_Leave_Provision_By_Sign_Into_Credit()
    {
        ArrangeLine("JV16MFM-2", "1500", "3001", 80, BillingType.Provision, "90003", "90004");

        var result = await _appService.GetPayrollJVCustom016MainFormatMybReport(Input());

        var summaryRow = result.Items.Single(r => r.CostCenter == "0000" && r.Exp == "090004");
        summaryRow.Credit.ShouldBe(80);
        summaryRow.Debit.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Be_Empty_When_No_Payroll_Area_Name_Contains_Myb()
    {
        var result = await _appService.GetPayrollJVCustom016MainFormatMybReport(Input());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Match_Payroll_Area_By_Name_Containment_Case_Insensitively()
    {
        ArrangeLine("JV16MFM-CI", "1600", "2000", 50, BillingType.Earning, "90005", "90006", "myb-kuwait");

        var result = await _appService.GetPayrollJVCustom016MainFormatMybReport(Input());

        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Export_To_Excel_With_The_Full_Filtered_Set_Not_Just_One_Page()
    {
        for (var i = 0; i < 3; i++)
        {
            ArrangeLine("JV16MFM-EXP-" + i, "1700", "2000", 100 + i, BillingType.Earning, "9100" + i, "9200" + i);
        }

        var file = await _appService.GetPayrollJVCustom016MainFormatMybReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom016MainFormatMyb.xlsx");
        file.FileType.ShouldNotBeNullOrEmpty();
    }
}
