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
/// Tests for the Employee Bank grid report (legacy <c>uscRptEmployeeBank.ascx</c>, <c>Table1()</c>),
/// including its report-specific payroll-area filter.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeBankReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeBankReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeBankReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeBankReportAppService>();
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
                CivilID = 123456789,
                Status = status
            }).Entity;
        });

        return employee;
    }

    private Bank CreateBank(string code)
    {
        Bank bank = null;
        UsingDbContext(context =>
        {
            bank = context.Set<Bank>().Add(new Bank
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
        });

        return bank;
    }

    private void CreateEmployeeBank(Employee employee, Bank bank, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeBank>().Add(new EmployeeBank
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                BankId = bank.Id,
                StartDate = start,
                EndDate = end,
                PaymentType = PaymentType.Bank,
                AccountNum = "ACC-" + employee.Code,
                IBAN = "IBAN-" + employee.Code
            });
        });
    }

    /// <summary>Creates a PayrollArea with the given code and an assignment covering the window.</summary>
    private PayrollArea CreatePayrollAreaAssignment(Employee employee, string payrollAreaCode, DateTime start, DateTime end)
    {
        PayrollArea payrollArea = null;
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;

            var payrollPeriod = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "P",
                NameAr = "P"
            }).Entity;

            context.SaveChanges();

            payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = payrollAreaCode,
                Name = "PA-" + payrollAreaCode,
                CurrencyId = currency.Id,
                PayrollPeriodId = payrollPeriod.Id
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                PayrollAreaId = payrollArea.Id
            });
        });

        return payrollArea;
    }

    private static GetEmployeeBankReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        string payrollAreaCode = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeBankReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            PayrollAreaCode = payrollAreaCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Bank_Records_Overlapping_The_Requested_Window()
    {
        var bank = CreateBank("BK-OVL");
        var inRange = CreateEmployee("BOVL-IN");
        var before = CreateEmployee("BOVL-BEFORE");

        CreateEmployeeBank(inRange, bank, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateEmployeeBank(before, bank, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));

        var result = await _appService.GetEmployeeBankReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("BOVL-IN");
        codes.ShouldNotContain("BOVL-BEFORE");
    }

    [Fact]
    public async Task Should_Include_Bank_Records_Touching_The_Window_Boundary()
    {
        var bank = CreateBank("BK-EDGE");
        var startsOnToDate = CreateEmployee("BEDGE-START");
        var endsOnFromDate = CreateEmployee("BEDGE-END");

        // Legacy predicate is NOT(StartDate > To || EndDate < From), so both boundary rows are included.
        CreateEmployeeBank(startsOnToDate, bank, new DateTime(2024, 12, 31), new DateTime(2025, 12, 31));
        CreateEmployeeBank(endsOnFromDate, bank, new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        var result = await _appService.GetEmployeeBankReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("BEDGE-START");
        codes.ShouldContain("BEDGE-END");
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Area_Using_The_Assignment_Covering_The_To_Date()
    {
        var bank = CreateBank("BK-PA");
        var matching = CreateEmployee("BPA-MATCH");
        var other = CreateEmployee("BPA-OTHER");

        CreateEmployeeBank(matching, bank, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeBank(other, bank, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var payrollAreaCode = "PA-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        // Covers the TO date (31/12/2024) -- this is the assignment legacy consults.
        CreatePayrollAreaAssignment(matching, payrollAreaCode, new DateTime(2024, 1, 1), new DateTime(2025, 12, 31));
        // Same payroll area but the assignment ENDS before the TO date, so legacy does not match it.
        CreatePayrollAreaAssignment(other, payrollAreaCode + "-X", new DateTime(2024, 1, 1), new DateTime(2024, 6, 30));

        var result = await _appService.GetEmployeeBankReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), payrollAreaCode: payrollAreaCode));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("BPA-MATCH");
        codes.ShouldNotContain("BPA-OTHER");
    }

    [Fact]
    public async Task Should_Project_The_Legacy_Bank_Columns()
    {
        var bank = CreateBank("BK-PROJ");
        var employee = CreateEmployee("BPROJ-1");

        CreateEmployeeBank(employee, bank, new DateTime(2024, 2, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeBankReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "BPROJ-1"));

        var row = result.Items.Single();
        row.EmployeeName.ShouldBe("Full BPROJ-1");
        row.BankName.ShouldBe("BK-PROJ-En");
        row.CivilId.ShouldBe(123456789);
        // Legacy's "EffectiveDate" column is the record's StartDate.
        row.EffectiveDate.ShouldBe(new DateTime(2024, 2, 1));
        row.PaymentType.ShouldBe(PaymentType.Bank);
        row.AccountNum.ShouldBe("ACC-BPROJ-1");
        row.IBAN.ShouldBe("IBAN-BPROJ-1");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var bank = CreateBank("BK-EXP");
        var first = CreateEmployee("BEXP-A");
        var second = CreateEmployee("BEXP-B");
        var third = CreateEmployee("BEXP-C");
        var excluded = CreateEmployee("BEXP-OUT");

        CreateEmployeeBank(first, bank, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeBank(second, bank, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeBank(third, bank, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeBank(excluded, bank, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            employeeCode: "BEXP-A,BEXP-B,BEXP-C",
            maxResultCount: 1);

        var file = await _appService.GetEmployeeBankReportToExcel(input);

        file.FileName.ShouldBe("EmployeeBank.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "BEXP-A", "BEXP-B", "BEXP-C" });
        exported.First().Keys.ShouldContain("IBAN");
    }
}
