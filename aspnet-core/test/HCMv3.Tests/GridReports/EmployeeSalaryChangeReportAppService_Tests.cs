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
/// Tests for the Employee Salary Change grid-report port (legacy
/// <c>uscRptEmployeeSalaryChange.ascx</c>, <c>Table1()</c> + <c>GetComponents()</c>).
///
/// This is the PIVOT report of the wave, so the tests pin the two things a pivot can silently get wrong:
/// WHICH two basic-pay records get compared (a strict "before" and an inclusive "on or before"), and the
/// zero-fill for a wage type missing from one of them.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeSalaryChangeReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeSalaryChangeReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeSalaryChangeReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSalaryChangeReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = NewCode(),
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
                IncludeInBasicPay = true
            }).Entity;
        });

        return wageType;
    }

    private void CreateBasicPay(
        Employee employee,
        DateTime startDate,
        params (WageType WageType, double Amount)[] lines)
    {
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = NewCode().Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;

            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = startDate,
                EndDate = startDate.AddYears(5),
                Amount = lines.Sum(l => l.Amount)
            }).Entity;

            context.SaveChanges();

            foreach (var line in lines)
            {
                context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
                {
                    TenantId = TenantId,
                    EmployeeBasicPayId = basicPay.Id,
                    WageTypeId = line.WageType.Id,
                    CurrencyId = currency.Id,
                    Amount = line.Amount
                });
            }

            context.SaveChanges();
        });
    }

    private static GetEmployeeSalaryChangeReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeSalaryChangeReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Compare_The_Pay_Before_The_Window_With_The_Pay_At_Its_End()
    {
        var basic = CreateWageType("1000");
        var employee = CreateEmployee("SC-1");

        CreateBasicPay(employee, new DateTime(2023, 1, 1), (basic, 1000));
        CreateBasicPay(employee, new DateTime(2024, 6, 1), (basic, 1200));

        var result = await _appService.GetEmployeeSalaryChangeReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "SC-1"));

        var row = result.Items.Single();
        var component = row.Components.Single(c => c.WageTypeCode == "1000");

        component.OldAmount.ShouldBe(1000);
        component.FinalAmount.ShouldBe(1200);
        component.AdjustmentAmount.ShouldBe(200);

        row.OldTotal.ShouldBe(1000);
        row.FinalTotal.ShouldBe(1200);
        row.AdjustmentTotal.ShouldBe(200);
        row.ParameterStartDate.ShouldBe(new DateTime(2024, 1, 1));
        row.ParameterEndDate.ShouldBe(new DateTime(2024, 12, 31));
    }

    [Fact]
    public async Task Should_Treat_A_Pay_Starting_Exactly_On_The_From_Date_As_Final_But_Not_As_Old()
    {
        var basic = CreateWageType("1000");
        var employee = CreateEmployee("SCEDGE-1");

        CreateBasicPay(employee, new DateTime(2023, 5, 1), (basic, 800));
        // Legacy "BEFORE" is StartDate < dtFrom (strict) while "AFTER" is StartDate <= dtTo (inclusive),
        // so this record is the Final one and the 2023 record stays the Old one.
        CreateBasicPay(employee, new DateTime(2024, 1, 1), (basic, 900));

        var result = await _appService.GetEmployeeSalaryChangeReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "SCEDGE-1"));

        var component = result.Items.Single().Components.Single(c => c.WageTypeCode == "1000");
        component.OldAmount.ShouldBe(800);
        component.FinalAmount.ShouldBe(900);
    }

    [Fact]
    public async Task Should_Ignore_Pay_Records_Starting_After_The_To_Date()
    {
        var basic = CreateWageType("1000");
        var employee = CreateEmployee("SCAFTER-1");

        CreateBasicPay(employee, new DateTime(2023, 1, 1), (basic, 500));
        CreateBasicPay(employee, new DateTime(2025, 1, 1), (basic, 5000));

        var result = await _appService.GetEmployeeSalaryChangeReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "SCAFTER-1"));

        var component = result.Items.Single().Components.Single(c => c.WageTypeCode == "1000");
        component.FinalAmount.ShouldBe(500);
        component.AdjustmentAmount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Zero_Fill_A_Wage_Type_Missing_From_One_Of_The_Two_Pays()
    {
        var basic = CreateWageType("1000");
        var newAllowance = CreateWageType("1200");
        var employee = CreateEmployee("SCFILL-1");

        CreateBasicPay(employee, new DateTime(2023, 1, 1), (basic, 1000));
        CreateBasicPay(employee, new DateTime(2024, 6, 1), (basic, 1000), (newAllowance, 300));

        var result = await _appService.GetEmployeeSalaryChangeReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "SCFILL-1"));

        var row = result.Items.Single();
        var allowance = row.Components.Single(c => c.WageTypeCode == "1200");

        allowance.OldAmount.ShouldBe(0);
        allowance.FinalAmount.ShouldBe(300);
        allowance.AdjustmentAmount.ShouldBe(300);
        row.AdjustmentTotal.ShouldBe(300);

        // The basic wage type is forced to the front of the pivot (legacy ordered code "1000" first).
        row.Components.First().WageTypeCode.ShouldBe("1000");
    }

    [Fact]
    public async Task Should_Only_Include_Employees_That_Have_A_Basic_Pay_Record()
    {
        var basic = CreateWageType("1000");
        var withPay = CreateEmployee("SCROWS-WITH");
        CreateEmployee("SCROWS-WITHOUT");

        CreateBasicPay(withPay, new DateTime(2023, 1, 1), (basic, 100));

        var result = await _appService.GetEmployeeSalaryChangeReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
                employeeCode: "SCROWS-WITH,SCROWS-WITHOUT"));

        result.Items.Select(i => i.EmployeeCode).ShouldBe(new List<string> { "SCROWS-WITH" });
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_With_The_Dynamic_Wage_Type_Columns()
    {
        var basic = CreateWageType("1000");
        var first = CreateEmployee("SCEXP-A");
        var second = CreateEmployee("SCEXP-B");
        var third = CreateEmployee("SCEXP-C");
        var excluded = CreateEmployee("SCEXP-OUT");

        foreach (var employee in new[] { first, second, third, excluded })
        {
            CreateBasicPay(employee, new DateTime(2023, 1, 1), (basic, 700));
        }

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var file = await _appService.GetEmployeeSalaryChangeReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
                employeeCode: "SCEXP-A,SCEXP-B,SCEXP-C", maxResultCount: 1));

        file.FileName.ShouldBe("EmployeeSalaryChange.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c)
            .ShouldBe(new List<string> { "SCEXP-A", "SCEXP-B", "SCEXP-C" });

        // The pivot columns are data-driven, so they must appear as literal wage-type headers.
        exported.First().Keys.ShouldContain("Old WT-1000");
        exported.First().Keys.ShouldContain("Adj WT-1000");
        exported.First().Keys.ShouldContain("Final WT-1000");
    }
}
