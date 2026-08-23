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
/// Tests for the Employee Status grid-report port (legacy <c>uscRptEmployeeStatus.ascx</c>,
/// <c>Table1()</c>). The report's whole point is the DERIVED lifecycle label, so most of these tests pin
/// one rung of the legacy first-match ladder.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeStatusReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeStatusReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeStatusReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeStatusReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private Employee CreateEmployee(
        string code,
        EmployeeStatus status = EmployeeStatus.Active,
        DateTime? joiningDate = null,
        DateTime? probationEndDate = null)
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
                // Defaults deliberately land OUTSIDE every ladder rung, so a row with no extra setup
                // comes back with an empty label.
                JoiningDate = joiningDate ?? new DateTime(2015, 1, 1),
                ProbationEndDate = probationEndDate ?? new DateTime(2015, 4, 1),
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });

        return employee;
    }

    private void CreateSeparation(Employee employee)
    {
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
                SubmissionDate = new DateTime(2024, 1, 1),
                LastDayInService = new DateTime(2024, 2, 1),
                PlannedLastDayInService = new DateTime(2024, 2, 1),
                SeparationReasonId = reason.Id
            });
        });
    }

    private void CreateCurrentBasicPay(
        Employee employee,
        double headerAmount,
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
                Amount = headerAmount
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

    private static GetEmployeeStatusReportInput Input(
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeStatusReportInput
        {
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Label_A_Separated_Employee_As_Separation()
    {
        var employee = CreateEmployee("ESTAT-SEP");
        CreateSeparation(employee);

        var result = await _appService.GetEmployeeStatusReport(Input(employeeCode: "ESTAT-SEP"));

        result.Items.Single().StatusLabel.ShouldBe("Separation");
    }

    [Fact]
    public async Task Should_Label_An_Employee_Still_On_Probation_As_Probationary()
    {
        CreateEmployee("ESTAT-PROB", probationEndDate: DateTime.Now.Date.AddMonths(1));

        var result = await _appService.GetEmployeeStatusReport(Input(employeeCode: "ESTAT-PROB"));

        result.Items.Single().StatusLabel.ShouldBe("Probationary");
    }

    [Fact]
    public async Task Should_Label_An_Employee_Joined_This_Calendar_Year_As_New_Join()
    {
        CreateEmployee("ESTAT-NEW", joiningDate: new DateTime(DateTime.Now.Year, 1, 2));

        var result = await _appService.GetEmployeeStatusReport(Input(employeeCode: "ESTAT-NEW"));

        result.Items.Single().StatusLabel.ShouldBe("New Join");
    }

    [Fact]
    public async Task Should_Leave_The_Label_Empty_When_No_Ladder_Rung_Matches()
    {
        CreateEmployee("ESTAT-NONE");

        var result = await _appService.GetEmployeeStatusReport(Input(employeeCode: "ESTAT-NONE"));

        var row = result.Items.Single();
        row.StatusLabel.ShouldBe(string.Empty);
        row.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Prefer_Separation_Over_Probationary_And_New_Join()
    {
        // All three rungs would match; legacy's ladder stops at the first one.
        var employee = CreateEmployee(
            "ESTAT-LADDER",
            joiningDate: new DateTime(DateTime.Now.Year, 1, 2),
            probationEndDate: DateTime.Now.Date.AddMonths(1));

        CreateSeparation(employee);

        var result = await _appService.GetEmployeeStatusReport(Input(employeeCode: "ESTAT-LADDER"));

        result.Items.Single().StatusLabel.ShouldBe("Separation");
    }

    [Fact]
    public async Task Should_Split_BasicPay_Allowance_And_Use_The_Header_Amount_As_TotalSalary()
    {
        var employee = CreateEmployee("ESTAT-PAY");

        // Header amount is deliberately NOT the sum of the detail lines: legacy line 648 reads the header.
        CreateCurrentBasicPay(employee, headerAmount: 2000,
            ("1000", 1000, true),
            ("1200", 250, true),
            ("9000", 75, false));

        var result = await _appService.GetEmployeeStatusReport(Input(employeeCode: "ESTAT-PAY"));

        var row = result.Items.Single();
        row.BasicPay.ShouldBe(1000);
        row.Allowance.ShouldBe(250);
        row.TotalSalary.ShouldBe(2000);
    }

    [Fact]
    public async Task Should_Apply_The_Status_And_Code_Filters_That_Legacy_Left_Commented_Out()
    {
        CreateEmployee("ESTATF-ACTIVE");
        CreateEmployee("ESTATF-INACTIVE", EmployeeStatus.Inactive);

        var result = await _appService.GetEmployeeStatusReport(
            Input(employeeCode: "ESTATF-ACTIVE,ESTATF-INACTIVE", status: EmployeeStatus.Inactive));

        result.Items.Select(i => i.EmployeeCode).ShouldBe(new List<string> { "ESTATF-INACTIVE" });
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        CreateEmployee("ESTATEXP-A");
        CreateEmployee("ESTATEXP-B");
        CreateEmployee("ESTATEXP-C");
        CreateEmployee("ESTATEXP-OUT");

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var file = await _appService.GetEmployeeStatusReportToExcel(
            Input(employeeCode: "ESTATEXP-A,ESTATEXP-B,ESTATEXP-C", maxResultCount: 1));

        file.FileName.ShouldBe("EmployeeStatus.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c)
            .ShouldBe(new List<string> { "ESTATEXP-A", "ESTATEXP-B", "ESTATEXP-C" });
    }
}
