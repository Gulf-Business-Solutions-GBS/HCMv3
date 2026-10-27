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
/// Tests for the Employee Basic Pay grid report (legacy <c>uscRptEmployeeBasicPay.ascx</c>,
/// <c>Table1()</c>).
///
/// The three atypical behaviours are each pinned: the date filter is INERT (legacy never reads the
/// pickers), the wage-type columns are DYNAMIC, and the increment column is a cross-row calculation --
/// which is also why the "export ignores page size" test matters more here than anywhere else.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeBasicPayReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeBasicPayReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeBasicPayReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeBasicPayReportAppService>();
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

    private WageType CreateWageType(string code, bool includeInBasicPay = true, int payrollOrder = 1)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                PayrollOrder = payrollOrder,
                IncludeInBasicPay = includeInBasicPay
            }).Entity;
        });

        return wageType;
    }

    private Currency CreateCurrency()
    {
        Currency currency = null;
        UsingDbContext(context =>
        {
            currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur",
                CurrencyShortFormEn = "KWD",
                CurrencyShortFormAr = "د.ك"
            }).Entity;
        });

        return currency;
    }

    private void CreateBasicPay(
        Employee employee,
        DateTime start,
        DateTime end,
        double totalAmount,
        Currency currency,
        params (WageType WageType, double Amount)[] lines)
    {
        UsingDbContext(context =>
        {
            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                Amount = totalAmount
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
        });
    }

    private static GetEmployeeBasicPayReportInput Input(
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeBasicPayReportInput
        {
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Ignore_The_Date_Filter_Because_Legacy_Never_Read_It()
    {
        var currency = CreateCurrency();
        var basic = CreateWageType("1000");
        var employee = CreateEmployee("PDATE-1");

        CreateBasicPay(employee, new DateTime(2019, 1, 1), new DateTime(2019, 12, 31), 500, currency, (basic, 500));

        var input = Input(employeeCode: "PDATE-1");
        // A window that excludes the record entirely: legacy's Table1() has no date predicate at all,
        // so the row must still come back.
        input.FromDate = new DateTime(2024, 1, 1);
        input.ToDate = new DateTime(2024, 12, 31);

        var result = await _appService.GetEmployeeBasicPayReport(input);

        result.Items.Count.ShouldBe(1);
        result.Items.Single().StartDate.ShouldBe(new DateTime(2019, 1, 1));
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var currency = CreateCurrency();
        var basic = CreateWageType("1000");
        var active = CreateEmployee("PST-ACTIVE");
        var inactive = CreateEmployee("PST-INACTIVE", EmployeeStatus.Inactive);

        CreateBasicPay(active, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 500, currency, (basic, 500));
        CreateBasicPay(inactive, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 500, currency, (basic, 500));

        var result = await _appService.GetEmployeeBasicPayReport(Input(status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PST-INACTIVE");
        codes.ShouldNotContain("PST-ACTIVE");
    }

    [Fact]
    public async Task Should_Return_Wage_Lines_Ordered_By_Payroll_Order_And_Exclude_Basic_Pay_From_The_Allowance()
    {
        var currency = CreateCurrency();
        var basic = CreateWageType("1000", includeInBasicPay: true, payrollOrder: 1);
        var allowance = CreateWageType("AL-" + Guid.NewGuid().ToString("N").Substring(0, 6), true, 2);
        var excluded = CreateWageType("EX-" + Guid.NewGuid().ToString("N").Substring(0, 6), false, 3);

        var employee = CreateEmployee("PWAGE-1");
        CreateBasicPay(
            employee,
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            1300,
            currency,
            (excluded, 50),
            (allowance, 300),
            (basic, 1000));

        var result = await _appService.GetEmployeeBasicPayReport(Input(employeeCode: "PWAGE-1"));

        var row = result.Items.Single();
        row.WageAmounts.Select(w => w.WageTypeCode).ShouldBe(
            new List<string> { basic.Code, allowance.Code, excluded.Code });
        // Legacy line 626: everything except wage type "1000", and only wage types flagged IncludeInBasicPay.
        row.TotalAllowance.ShouldBe(300);
        // Legacy line 627: the record's own Amount, not a sum of the lines.
        row.TotalAmount.ShouldBe(1300);
        row.Currency.ShouldBe("KWD");
    }

    [Fact]
    public async Task Should_Compute_Increment_Against_The_Previous_Pay_Record_Of_The_Same_Employee()
    {
        var currency = CreateCurrency();
        var basic = CreateWageType("1000");
        var employee = CreateEmployee("PINC-1");

        CreateBasicPay(employee, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31), 1000, currency, (basic, 1000));
        CreateBasicPay(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 1200, currency, (basic, 1200));

        var result = await _appService.GetEmployeeBasicPayReport(Input(employeeCode: "PINC-1"));

        var newest = result.Items.Single(i => i.StartDate == new DateTime(2024, 1, 1));
        var oldest = result.Items.Single(i => i.StartDate == new DateTime(2023, 1, 1));

        newest.IncrementAmount.ShouldBe(200);
        // Legacy leaves the oldest record in a group at 0 (there is no previous amount).
        oldest.IncrementAmount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var currency = CreateCurrency();
        var basic = CreateWageType("1000");
        var first = CreateEmployee("PEXP-A");
        var second = CreateEmployee("PEXP-B");
        var third = CreateEmployee("PEXP-C");
        var excluded = CreateEmployee("PEXP-OUT");

        CreateBasicPay(first, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 500, currency, (basic, 500));
        CreateBasicPay(second, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 500, currency, (basic, 500));
        CreateBasicPay(third, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 500, currency, (basic, 500));
        CreateBasicPay(excluded, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 500, currency, (basic, 500));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(employeeCode: "PEXP-A,PEXP-B,PEXP-C", maxResultCount: 1);

        var page = await _appService.GetEmployeeBasicPayReport(input);
        page.TotalCount.ShouldBe(3);
        page.Items.Count.ShouldBe(1);

        var file = await _appService.GetEmployeeBasicPayReportToExcel(input);

        file.FileName.ShouldBe("EmployeeBasicPay.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "PEXP-A", "PEXP-B", "PEXP-C" });
        // The dynamic wage-type column is spliced in before the totals.
        exported.First().Keys.ShouldContain("1000-En");
        exported.First().Keys.ShouldContain("Increment Amount");
    }
}
