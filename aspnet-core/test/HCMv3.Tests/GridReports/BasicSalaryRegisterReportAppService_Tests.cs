using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Basic Salary Register grid report (legacy
/// <c>uscBasicSalaryRegister.ascx</c>, <c>Table1()</c> lines 421-585).
///
/// Each test pins one legacy semantic: the InPeriod window (both ends inclusive), the optional
/// PayrollRunType filter, the wage-type-code to column mapping, the always-empty PresentDays column
/// (a reproduced legacy defect), and the rule that the export ignores the page size.
/// </summary>
// ReSharper disable once InconsistentNaming
public class BasicSalaryRegisterReportAppService_Tests : AppTestBase
{
    private readonly IBasicSalaryRegisterReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public BasicSalaryRegisterReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IBasicSalaryRegisterReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
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
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private WageType CreateWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "WT-" + code,
                NameAr = "WT-" + code,
                ShortNameEn = code,
                ShortNameAr = code,
                Status = true
            }).Entity;
        });

        return wageType;
    }

    private SalaryRegister CreateRegister(
        Employee employee,
        DateTime inPeriod,
        PayrollRunType runType = PayrollRunType.Regular)
    {
        SalaryRegister register = null;
        UsingDbContext(context =>
        {
            register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = inPeriod,
                InPeriod = inPeriod,
                PayrollRunType = runType,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = inPeriod
            }).Entity;
        });

        return register;
    }

    private void CreateDetail(SalaryRegister register, WageType wageType, double amount)
    {
        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                Rate = 0,
                Number = 0
            });
        });
    }

    private static GetBasicSalaryRegisterReportInput Input(
        DateTime from,
        DateTime to,
        PayrollRunType? runType = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetBasicSalaryRegisterReportInput
        {
            FromDate = from,
            ToDate = to,
            PayrollRunType = runType,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Return_Only_Registers_Whose_InPeriod_Falls_In_The_Window()
    {
        var basicSalary = CreateWageType("1000");
        var inRange = CreateEmployee("BSR-IN");
        var before = CreateEmployee("BSR-BEFORE");
        var after = CreateEmployee("BSR-AFTER");

        CreateDetail(CreateRegister(inRange, new DateTime(2024, 6, 30)), basicSalary, 100);
        CreateDetail(CreateRegister(before, new DateTime(2023, 12, 31)), basicSalary, 100);
        CreateDetail(CreateRegister(after, new DateTime(2025, 1, 31)), basicSalary, 100);

        var result = await _appService.GetBasicSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("BSR-IN");
        codes.ShouldNotContain("BSR-BEFORE");
        codes.ShouldNotContain("BSR-AFTER");
    }

    [Fact]
    public async Task Should_Include_Registers_Sitting_Exactly_On_The_Window_Boundaries()
    {
        var basicSalary = CreateWageType("1000");
        var onFrom = CreateEmployee("BSR-EDGE-FROM");
        var onTo = CreateEmployee("BSR-EDGE-TO");

        // Legacy predicate is InPeriod <= dtTo && InPeriod >= dtFrom -- BOTH ends inclusive.
        CreateDetail(CreateRegister(onFrom, new DateTime(2024, 1, 1)), basicSalary, 100);
        CreateDetail(CreateRegister(onTo, new DateTime(2024, 12, 31)), basicSalary, 100);

        var result = await _appService.GetBasicSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("BSR-EDGE-FROM");
        codes.ShouldContain("BSR-EDGE-TO");
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Run_Type_When_Supplied()
    {
        var basicSalary = CreateWageType("1000");
        var regular = CreateEmployee("BSR-RT-REGULAR");
        var offCycle = CreateEmployee("BSR-RT-OFFCYCLE");

        CreateDetail(CreateRegister(regular, new DateTime(2024, 6, 30)), basicSalary, 100);
        CreateDetail(
            CreateRegister(offCycle, new DateTime(2024, 6, 30), PayrollRunType.OffCycle),
            basicSalary,
            100);

        var result = await _appService.GetBasicSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), runType: PayrollRunType.OffCycle));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("BSR-RT-OFFCYCLE");
        codes.ShouldNotContain("BSR-RT-REGULAR");
    }

    [Fact]
    public async Task Should_Map_Each_Legacy_Wage_Type_Code_To_Its_Own_Column()
    {
        var employee = CreateEmployee("BSR-MAP");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));

        // The thirteen wage types legacy projects (Table1 lines 530-546).
        CreateDetail(register, CreateWageType("9000"), 900);   // NetPay
        CreateDetail(register, CreateWageType("1000"), 1000);  // BasicSalary
        CreateDetail(register, CreateWageType("1003"), 1003);  // AnnualLeavePay
        CreateDetail(register, CreateWageType("2000"), 2000);  // LateEarlyDeduction
        CreateDetail(register, CreateWageType("2002"), 2002);  // PIFSSContributionDed
        CreateDetail(register, CreateWageType("3000"), 3000);  // GratuityProvision
        CreateDetail(register, CreateWageType("3001"), 3001);  // AnnualLeaveProvision
        CreateDetail(register, CreateWageType("4010"), 4010);  // AnnualLeaveCurrentMonthAccrual
        CreateDetail(register, CreateWageType("4011"), 4011);  // ActiveDaysInAMonth
        CreateDetail(register, CreateWageType("2007"), 2007);  // AbsentUnpaidDed
        CreateDetail(register, CreateWageType("1017"), 1017);  // MiscellaneousPay
        CreateDetail(register, CreateWageType("4021"), 4021);  // AnnualLeaveOpeningBal
        CreateDetail(register, CreateWageType("4022"), 4022);  // AnnualLeaveClosingBal

        var result = await _appService.GetBasicSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var row = result.Items.Single(i => i.EmployeeCode == "BSR-MAP");
        row.EmployeeName.ShouldBe("Full BSR-MAP");
        row.NetPay.ShouldBe(900);
        row.BasicSalary.ShouldBe(1000);
        row.AnnualLeavePay.ShouldBe(1003);
        row.LateEarlyDeduction.ShouldBe(2000);
        row.PifssContributionDed.ShouldBe(2002);
        row.GratuityProvision.ShouldBe(3000);
        row.AnnualLeaveProvision.ShouldBe(3001);
        row.AnnualLeaveCurrentMonthAccrual.ShouldBe(4010);
        row.ActiveDaysInAMonth.ShouldBe(4011);
        row.AbsentUnpaidDed.ShouldBe(2007);
        row.MiscellaneousPay.ShouldBe(1017);
        row.AnnualLeaveOpeningBal.ShouldBe(4021);
        row.AnnualLeaveClosingBal.ShouldBe(4022);
    }

    [Fact]
    public async Task Should_Report_Zero_For_A_Wage_Type_The_Register_Does_Not_Have()
    {
        var employee = CreateEmployee("BSR-MISSING");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));

        CreateDetail(register, CreateWageType("1000"), 500);

        var result = await _appService.GetBasicSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var row = result.Items.Single(i => i.EmployeeCode == "BSR-MISSING");
        row.BasicSalary.ShouldBe(500);
        // Legacy did `rec.X != null ? rec.X.Amount : 0` -- an absent wage type reads as 0, not null.
        row.NetPay.ShouldBe(0);
        row.GratuityProvision.ShouldBe(0);
    }

    /// <summary>
    /// LEGACY DEFECT, DELIBERATELY REPRODUCED: <c>uscBasicSalaryRegister.Table1()</c> declares the
    /// "PresentDays" column (line 452) and adds it to the DataTable, but never selects a wage type for it
    /// and never assigns <c>dr[PresentDays]</c> in the fill loop. The legacy grid therefore always showed
    /// PresentDays empty. The port keeps it at 0 rather than inventing a wage type for it.
    /// </summary>
    [Fact]
    public async Task Should_Always_Leave_PresentDays_Empty_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("BSR-PRESENT");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));

        CreateDetail(register, CreateWageType("1000"), 100);
        CreateDetail(register, CreateWageType("4011"), 30);

        var result = await _appService.GetBasicSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var row = result.Items.Single(i => i.EmployeeCode == "BSR-PRESENT");
        row.ActiveDaysInAMonth.ShouldBe(30);
        row.PresentDays.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Number_Rows_Continuing_Across_Pages()
    {
        var basicSalary = CreateWageType("1000");

        foreach (var code in new[] { "BSR-SN-A", "BSR-SN-B", "BSR-SN-C" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), basicSalary, 10);
        }

        var secondPage = await _appService.GetBasicSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), maxResultCount: 1, skipCount: 1));

        // Legacy SrNo was the position in the rendered table; with server paging it must continue from
        // the page offset rather than restarting at 1.
        secondPage.Items.Single().SerialNumber.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Page_Results()
    {
        var basicSalary = CreateWageType("1000");

        foreach (var code in new[] { "BSR-PAGE-A", "BSR-PAGE-B" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), basicSalary, 10);
        }

        var page = await _appService.GetBasicSalaryRegisterReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), maxResultCount: 1));

        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var basicSalary = CreateWageType("1000");

        foreach (var code in new[] { "BSR-EXP-A", "BSR-EXP-B", "BSR-EXP-C" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), basicSalary, 10);
        }

        // Out of the window -- must not reach the export.
        CreateDetail(
            CreateRegister(CreateEmployee("BSR-EXP-OUT"), new DateTime(2025, 6, 30)),
            basicSalary,
            10);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var input = Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), maxResultCount: 1);

        var file = await _appService.GetBasicSalaryRegisterReportToExcel(input);

        file.FileName.ShouldBe("BasicSalaryRegister.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "BSR-EXP-A", "BSR-EXP-B", "BSR-EXP-C" });

        // Serial numbers restart at 1 for the export because it returns the whole set.
        exported.Select(r => r["Sr. No."]?.ToString()).ShouldBe(new List<string> { "1", "2", "3" });
    }
}
