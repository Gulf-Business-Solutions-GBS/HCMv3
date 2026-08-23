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
/// Tests for the Employee Post Wise grid-report port (legacy <c>uscRptEmployeePostWise.ascx</c>,
/// <c>Table1()</c>). The report is DATE-LESS -- legacy hides both date pickers and keys every lookup on
/// "now" -- so the tests exercise the status/code filters, the wage aggregation and the derived columns.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeePostWiseReportAppService_Tests : AppTestBase
{
    private readonly IEmployeePostWiseReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeePostWiseReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeePostWiseReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private Employee CreateEmployee(
        string code,
        EmployeeStatus status = EmployeeStatus.Active,
        DateTime? birthDate = null,
        DateTime? joiningDate = null)
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
                BirthDate = birthDate ?? new DateTime(1990, 1, 1),
                JoiningDate = joiningDate ?? new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });

        return employee;
    }

    /// <summary>
    /// Basic pay current TODAY with one wage-type line per (code, amount) pair. Only the line whose code
    /// is "1000" counts as basic salary; the others count as allowance when flagged IncludeInBasicPay.
    /// </summary>
    private void CreateCurrentBasicPay(
        Employee employee,
        params (string WageTypeCode, double Amount, bool IncludeInBasicPay)[] lines)
    {
        var today = DateTime.Now.Date;

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
                StartDate = today.AddYears(-1),
                EndDate = today.AddYears(1),
                Amount = lines.Sum(l => l.Amount)
            }).Entity;

            context.SaveChanges();

            foreach (var line in lines)
            {
                var wageType = context.Set<WageType>().Add(new WageType
                {
                    TenantId = TenantId,
                    Code = line.WageTypeCode,
                    NameEn = "WT-" + line.WageTypeCode,
                    NameAr = "WT-" + line.WageTypeCode,
                    IncludeInBasicPay = line.IncludeInBasicPay
                }).Entity;

                context.SaveChanges();

                context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
                {
                    TenantId = TenantId,
                    EmployeeBasicPayId = basicPay.Id,
                    WageTypeId = wageType.Id,
                    CurrencyId = currency.Id,
                    Amount = line.Amount
                });

                context.SaveChanges();
            }
        });
    }

    private static GetEmployeePostWiseReportInput Input(
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeePostWiseReportInput
        {
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        CreateEmployee("PW-ACTIVE");
        CreateEmployee("PW-INACTIVE", EmployeeStatus.Inactive);

        var result = await _appService.GetEmployeePostWiseReport(
            Input(employeeCode: "PW-ACTIVE,PW-INACTIVE", status: EmployeeStatus.Inactive));

        result.Items.Select(i => i.EmployeeCode).ShouldBe(new List<string> { "PW-INACTIVE" });
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        CreateEmployee("PWCODE-A");
        CreateEmployee("PWCODE-B");
        CreateEmployee("PWCODE-C");

        var result = await _appService.GetEmployeePostWiseReport(Input(employeeCode: "PWCODE-A , PWCODE-C"));

        result.Items.Select(i => i.EmployeeCode).OrderBy(c => c)
            .ShouldBe(new List<string> { "PWCODE-A", "PWCODE-C" });
    }

    [Fact]
    public async Task Should_Split_Basic_Salary_Allowance_And_Total_The_Way_Legacy_Did()
    {
        var employee = CreateEmployee("PWPAY-1");

        // 1000 is basic; 1200 is an allowance (IncludeInBasicPay); 9000 is neither but still totals.
        CreateCurrentBasicPay(employee,
            ("1000", 1000, true),
            ("1200", 250, true),
            ("9000", 75, false));

        var result = await _appService.GetEmployeePostWiseReport(Input(employeeCode: "PWPAY-1"));

        var row = result.Items.Single();
        row.BasicSalary.ShouldBe(1000);
        row.Allowance.ShouldBe(250);
        // Legacy line 673 sums EVERY wage type, including the ones excluded from the allowance figure.
        row.TotalSalary.ShouldBe(1325);
    }

    [Fact]
    public async Task Should_Stamp_Age_And_Service_Duration_On_Every_Row()
    {
        var birthDate = DateTime.Now.Date.AddYears(-30);
        var joiningDate = DateTime.Now.Date.AddYears(-5);
        CreateEmployee("PWCALC-1", birthDate: birthDate, joiningDate: joiningDate);

        var result = await _appService.GetEmployeePostWiseReport(Input(employeeCode: "PWCALC-1"));

        var row = result.Items.Single();
        row.BirthDate.ShouldBe(birthDate);
        row.Age.ShouldBe(30, 0.2);
        // Still in service, so the duration runs to "now": ~5 years.
        row.ServiceDurationYears.ShouldBe(5, 0.2);
        row.LastWorkingDay.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Measure_Service_Duration_To_The_Last_Working_Day_When_Separated()
    {
        var joiningDate = DateTime.Now.Date.AddYears(-10);
        var employee = CreateEmployee("PWSEP-1", joiningDate: joiningDate);
        var lastDay = joiningDate.AddYears(4);

        UsingDbContext(context =>
        {
            var reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "R",
                NameAr = "R"
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SubmissionDate = joiningDate.AddYears(3),
                LastDayInService = lastDay,
                PlannedLastDayInService = lastDay,
                SeparationReasonId = reason.Id
            });
        });

        var result = await _appService.GetEmployeePostWiseReport(Input(employeeCode: "PWSEP-1"));

        var row = result.Items.Single();
        row.LastWorkingDay.ShouldBe(lastDay);
        row.ServiceDurationYears.ShouldBe(4, 0.2);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        CreateEmployee("PWEXP-A");
        CreateEmployee("PWEXP-B");
        CreateEmployee("PWEXP-C");
        CreateEmployee("PWEXP-OUT");

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var file = await _appService.GetEmployeePostWiseReportToExcel(
            Input(employeeCode: "PWEXP-A,PWEXP-B,PWEXP-C", maxResultCount: 1));

        file.FileName.ShouldBe("EmployeePostWise.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c)
            .ShouldBe(new List<string> { "PWEXP-A", "PWEXP-B", "PWEXP-C" });
    }
}
