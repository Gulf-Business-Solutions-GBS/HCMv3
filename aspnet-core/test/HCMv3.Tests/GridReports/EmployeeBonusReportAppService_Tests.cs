using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Bonus grid-report port (legacy <c>uscRptEmployeeBonus.ascx</c>, <c>Table1()</c>).
/// Each test pins one legacy semantic: the dynamic year pivot, the non-zero-years average denominator, the
/// employee filters and the "export ignores page size" rule.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeBonusReportAppService_Tests : AppTestBase
{
    private const string BonusWageTypeCode = "1053";

    private readonly IEmployeeBonusReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeBonusReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeBonusReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
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
                Status = status
            }).Entity;
        });

        return employee;
    }

    private WageType CreateBonusWageType()
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().FirstOrDefault(w => w.Code == BonusWageTypeCode);
            if (wageType == null)
            {
                wageType = context.Set<WageType>().Add(new WageType
                {
                    TenantId = TenantId,
                    Code = BonusWageTypeCode,
                    NameEn = "Bonus Pay",
                    NameAr = "Bonus Pay Ar"
                }).Entity;
            }
        });

        return wageType;
    }

    private void CreateBonus(Employee employee, WageType wageType, DateTime paymentDate, double amount)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeNonPayrollTrans>().Add(new EmployeeNonPayrollTrans
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                WageTypeId = wageType.Id,
                PaymentDate = paymentDate,
                Amount = amount
            });
        });
    }

    private static GetEmployeeBonusReportInput Input(
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeeBonusReportInput
        {
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Generate_One_Column_Per_Year_Between_The_Min_And_Max_Payment_Year()
    {
        var wageType = CreateBonusWageType();
        var employee = CreateEmployee("BON-YEARS");

        // A gap year (2021) must still get a column: legacy walked min..max, it did not use the distinct set.
        CreateBonus(employee, wageType, new DateTime(2020, 5, 1), 100);
        CreateBonus(employee, wageType, new DateTime(2022, 5, 1), 300);

        var result = await _appService.GetEmployeeBonusReport(Input(employeeCode: "BON-YEARS"));

        result.Years.ShouldContain(2020);
        result.Years.ShouldContain(2021);
        result.Years.ShouldContain(2022);

        var row = result.Items.Single();
        row.YearlyAmounts.Count.ShouldBe(result.Years.Count);
        row.YearlyAmounts.Single(y => y.Year == 2020).Amount.ShouldBe(100);
        row.YearlyAmounts.Single(y => y.Year == 2021).Amount.ShouldBe(0);
        row.YearlyAmounts.Single(y => y.Year == 2022).Amount.ShouldBe(300);
    }

    [Fact]
    public async Task Should_Average_Over_Non_Zero_Years_Only()
    {
        var wageType = CreateBonusWageType();
        var employee = CreateEmployee("BON-AVG");

        // Two paid years plus (at least) one zero year in between; legacy divides by 2, not by 3.
        CreateBonus(employee, wageType, new DateTime(2020, 5, 1), 100);
        CreateBonus(employee, wageType, new DateTime(2022, 5, 1), 300);

        var result = await _appService.GetEmployeeBonusReport(Input(employeeCode: "BON-AVG"));

        var row = result.Items.Single();
        row.AverageBonusAmount.ShouldBe(200);
        row.MonthlyAverageBonusAmount.ShouldBe(200d / 12);
    }

    [Fact]
    public async Task Should_Include_Employees_With_No_Bonus_At_All()
    {
        var wageType = CreateBonusWageType();
        var withBonus = CreateEmployee("BON-HAS");
        CreateEmployee("BON-NONE");

        CreateBonus(withBonus, wageType, new DateTime(2020, 5, 1), 100);

        var result = await _appService.GetEmployeeBonusReport(Input(employeeCode: "BON-NONE"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("BON-NONE");
        row.AverageBonusAmount.ShouldBe(0);
        row.MonthlyAverageBonusAmount.ShouldBe(0);
        row.YearlyAmounts.ShouldAllBe(y => y.Amount == 0);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        CreateBonusWageType();
        CreateEmployee("BON-ST-ACTIVE");
        CreateEmployee("BON-ST-INACTIVE", EmployeeStatus.Inactive);

        var result = await _appService.GetEmployeeBonusReport(
            Input(employeeCode: "BON-ST-ACTIVE,BON-ST-INACTIVE", status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("BON-ST-INACTIVE");
        codes.ShouldNotContain("BON-ST-ACTIVE");
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        CreateBonusWageType();
        CreateEmployee("BON-CODE-A");
        CreateEmployee("BON-CODE-B");
        CreateEmployee("BON-CODE-C");

        // Whitespace around the separator is tolerated (legacy did a bare Split(',')).
        var result = await _appService.GetEmployeeBonusReport(Input(employeeCode: "BON-CODE-A , BON-CODE-C"));

        var codes = result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "BON-CODE-A", "BON-CODE-C" });
    }

    [Fact]
    public async Task Should_Page_Results()
    {
        CreateBonusWageType();
        CreateEmployee("BON-PAGE-A");
        CreateEmployee("BON-PAGE-B");

        var page = await _appService.GetEmployeeBonusReport(
            Input(employeeCode: "BON-PAGE-A,BON-PAGE-B", maxResultCount: 1));

        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(1);
        page.Items.Single().EmployeeCode.ShouldBe("BON-PAGE-A"); // default sort is EmployeeCode ASC
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var wageType = CreateBonusWageType();
        var first = CreateEmployee("BON-EXP-A");
        CreateEmployee("BON-EXP-B");
        CreateEmployee("BON-EXP-C");
        CreateEmployee("BON-EXP-OUT");

        CreateBonus(first, wageType, new DateTime(2020, 5, 1), 100);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var input = Input(employeeCode: "BON-EXP-A,BON-EXP-B,BON-EXP-C", maxResultCount: 1);

        var file = await _appService.GetEmployeeBonusReportToExcel(input);

        file.FileName.ShouldBe("EmployeeBonus.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "BON-EXP-A", "BON-EXP-B", "BON-EXP-C" });

        // The pivot columns must be in the sheet too, headed by the bare year.
        exported.First().Keys.ShouldContain("2020");
    }
}
