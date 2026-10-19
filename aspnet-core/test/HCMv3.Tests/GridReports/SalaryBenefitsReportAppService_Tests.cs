using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.Workflow;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Salary Benefits grid-report port (legacy <c>uscRptSalaryBenefits.ascx</c>,
/// <c>Table1()</c> lines 514-885).
///
/// The distinctive thing to pin here is the DYNAMIC wage-type column set: the server must return the
/// column list and every row must carry an amount for each column, zero-filled where the employee has no
/// detail row for that wage type (legacy line 834). The "export ignores page size" test is kept and also
/// asserts that the dynamic columns reach the spreadsheet.
/// </summary>
// ReSharper disable once InconsistentNaming
public class SalaryBenefitsReportAppService_Tests : AppTestBase
{
    private readonly ISalaryBenefitsReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public SalaryBenefitsReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISalaryBenefitsReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static DateTime Yesterday => DateTime.Now.Date.AddDays(-1);

    private static DateTime Tomorrow => DateTime.Now.Date.AddDays(1);

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

    private int CreateWageType(string nameEn, int payrollOrder)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn + "-Ar",
                PayrollOrder = payrollOrder
            }).Entity;
            context.SaveChanges();
            id = wageType.Id;
        });

        return id;
    }

    private int CreateCurrency()
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;
            context.SaveChanges();
            id = currency.Id;
        });

        return id;
    }

    /// <summary>Creates a basic pay CURRENT TODAY plus one detail row per supplied wage type.</summary>
    private void CreateCurrentBasicPay(Employee employee, double total, params (int WageTypeId, double Amount)[] details)
    {
        var currencyId = CreateCurrency();

        UsingDbContext(context =>
        {
            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = Yesterday,
                EndDate = Tomorrow,
                Amount = total
            }).Entity;

            context.SaveChanges();

            foreach (var detail in details)
            {
                context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
                {
                    TenantId = TenantId,
                    EmployeeBasicPayId = basicPay.Id,
                    WageTypeId = detail.WageTypeId,
                    Amount = detail.Amount,
                    CurrencyId = currencyId
                });
            }

            context.SaveChanges();
        });
    }

    private static GetSalaryBenefitsReportInput Input(
        string employeeCode = null,
        EmployeeStatus? status = null,
        string payrollAreaCode = null,
        int maxResultCount = 100)
    {
        return new GetSalaryBenefitsReportInput
        {
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            PayrollAreaCode = payrollAreaCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Project_The_Core_Employee_Columns()
    {
        var employee = CreateEmployee("SB-PROJ");

        var result = await _appService.GetSalaryBenefitsReport(Input(employeeCode: "SB-PROJ"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("SB-PROJ");
        // Legacy always wrote FullNameEn into this column, even in the Arabic aspect.
        row.EmployeeName.ShouldBe("Full SB-PROJ");
        row.Status.ShouldBe(EmployeeStatus.Active.ToString());
        row.JoiningDate.ShouldBe(new DateTime(2020, 1, 1));
        row.ServiceDuration.ShouldNotBeNullOrWhiteSpace();
        row.Age.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        CreateEmployee("SB-ST-ACTIVE");
        CreateEmployee("SB-ST-INACTIVE", EmployeeStatus.Inactive);

        var result = await _appService.GetSalaryBenefitsReport(
            Input(employeeCode: "SB-ST-ACTIVE,SB-ST-INACTIVE", status: EmployeeStatus.Inactive));

        result.Items.Single().EmployeeCode.ShouldBe("SB-ST-INACTIVE");
    }

    [Fact]
    public async Task Should_Return_The_Dynamic_Wage_Type_Columns_And_Zero_Fill_Missing_Ones()
    {
        var basic = CreateWageType("SB Basic", 1);
        var housing = CreateWageType("SB Housing", 2);

        var withBoth = CreateEmployee("SB-WT-BOTH");
        var withBasicOnly = CreateEmployee("SB-WT-BASIC");

        CreateCurrentBasicPay(withBoth, 1500, (basic, 1000), (housing, 500));
        CreateCurrentBasicPay(withBasicOnly, 900, (basic, 900));

        var result = await _appService.GetSalaryBenefitsReport(
            Input(employeeCode: "SB-WT-BOTH,SB-WT-BASIC"));

        result.WageTypeNames.ShouldContain("SB Basic");
        result.WageTypeNames.ShouldContain("SB Housing");

        var both = result.Items.Single(r => r.EmployeeCode == "SB-WT-BOTH");
        both.TotalSalary.ShouldBe(1500);
        both.EffectiveDate.ShouldBe(Yesterday);
        both.WageAmounts["SB Basic"].ShouldBe(1000);
        both.WageAmounts["SB Housing"].ShouldBe(500);

        // Legacy wrote 0, not blank, for a wage type the employee has no detail row for.
        var basicOnly = result.Items.Single(r => r.EmployeeCode == "SB-WT-BASIC");
        basicOnly.WageAmounts["SB Basic"].ShouldBe(900);
        basicOnly.WageAmounts["SB Housing"].ShouldBe(0);
    }

    [Fact]
    public async Task Should_Leave_Salary_Columns_Empty_When_There_Is_No_Current_Basic_Pay()
    {
        var employee = CreateEmployee("SB-NOPAY");

        var result = await _appService.GetSalaryBenefitsReport(Input(employeeCode: "SB-NOPAY"));

        var row = result.Items.Single();
        row.EffectiveDate.ShouldBeNull();
        row.TotalSalary.ShouldBeNull();
        // Legacy skipped the whole wage block behind its "if (empBasicSalary != null)" guard.
        row.WageAmounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var basic = CreateWageType("SB Export Basic", 1);

        var first = CreateEmployee("SB-EXP-A");
        var second = CreateEmployee("SB-EXP-B");
        var third = CreateEmployee("SB-EXP-C");
        CreateEmployee("SB-EXP-OUT");

        CreateCurrentBasicPay(first, 100, (basic, 100));
        CreateCurrentBasicPay(second, 200, (basic, 200));
        CreateCurrentBasicPay(third, 300, (basic, 300));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var file = await _appService.GetSalaryBenefitsReportToExcel(
            Input(employeeCode: "SB-EXP-A,SB-EXP-B,SB-EXP-C", maxResultCount: 1));

        file.FileName.ShouldBe("SalaryBenefits.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "SB-EXP-A", "SB-EXP-B", "SB-EXP-C" });
        // The dynamic wage-type column must reach the spreadsheet, not just the grid.
        exported.First().Keys.ShouldContain("SB Export Basic");
        // The exporter writes LOCALIZED header cells, not the raw localization key.
        exported.First().Keys.ShouldContain("Total Salary");
    }

    // --- Workflow Group column -------------------------------------------------------------------------

    private int CreateWorkflowGroup(string code)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var group = context.Set<WorkflowGroup>().Add(new WorkflowGroup
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + " En",
                NameAr = code + " Ar"
            }).Entity;
            context.SaveChanges();
            id = group.Id;
        });

        return id;
    }

    private void CreateAssignmentWithWorkflowGroup(Employee employee, DateTime start, DateTime end, int? workflowGroupId)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                WorkflowGroupId = workflowGroupId
            });
        });
    }

    private static async Task<TResult> WithUiCulture<TResult>(string cultureName, Func<Task<TResult>> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
        try
        {
            return await action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public async Task Should_Show_The_Workflow_Group_Of_Todays_Assignment()
    {
        var today = DateTime.Now.Date;
        var employee = CreateEmployee("SB-WFG");
        var oldGroup = CreateWorkflowGroup("SB-WFG-OLD");
        var currentGroup = CreateWorkflowGroup("SB-WFG-CUR");

        CreateAssignmentWithWorkflowGroup(employee, today.AddYears(-3), today.AddYears(-1), oldGroup);
        CreateAssignmentWithWorkflowGroup(employee, today.AddYears(-1).AddDays(1), today.AddYears(1), currentGroup);

        var result = await WithUiCulture("en", () => _appService.GetSalaryBenefitsReport(Input(employeeCode: "SB-WFG")));

        result.Items.Single().WorkflowGroupName.ShouldBe("SB-WFG-CUR En");
    }

    [Fact]
    public async Task Should_Keep_The_English_Workflow_Group_Name_For_An_Arabic_Request()
    {
        // Legacy line 845 wrote WorkflowGroup.NameEn in every language.
        var employee = CreateEmployee("SB-WFG-AR");
        var group = CreateWorkflowGroup("SB-WFG-AR");
        CreateAssignmentWithWorkflowGroup(employee, DateTime.Now.Date.AddYears(-1), DateTime.Now.Date.AddYears(1), group);

        var result = await WithUiCulture("ar", () => _appService.GetSalaryBenefitsReport(Input(employeeCode: "SB-WFG-AR")));

        result.Items.Single().WorkflowGroupName.ShouldBe("SB-WFG-AR En");
    }

    [Fact]
    public async Task Should_Export_The_Workflow_Group_Column()
    {
        var employee = CreateEmployee("SB-WFG-EXP");
        var group = CreateWorkflowGroup("SB-WFG-EXP");
        CreateAssignmentWithWorkflowGroup(employee, DateTime.Now.Date.AddYears(-1), DateTime.Now.Date.AddYears(1), group);

        var file = await WithUiCulture("en", () => _appService.GetSalaryBenefitsReportToExcel(Input(employeeCode: "SB-WFG-EXP")));

        using var stream = new MemoryStream(_tempFileCacheManager.GetFile(file.FileToken));
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Single()["Workflow Group"]?.ToString().ShouldBe("SB-WFG-EXP En");
    }
}
