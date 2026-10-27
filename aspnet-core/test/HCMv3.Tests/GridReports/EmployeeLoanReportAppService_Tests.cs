using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Transaction;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EmployeeLoanReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeLoanReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeLoanReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeLoanReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality { TenantId = TenantId, Code = NewCode(), NameEn = "N", NameAr = "N" }).Entity;
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
                JoiningDate = new DateTime(2015, 1, 1),
                ProbationEndDate = new DateTime(2015, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee;
    }

    private LoanType CreateLoanType(string code)
    {
        LoanType loanType = null;
        UsingDbContext(context =>
        {
            loanType = context.Set<LoanType>().Add(new LoanType { TenantId = TenantId, Code = code, NameEn = code + "-En", NameAr = code + "-Ar" }).Entity;
        });
        return loanType;
    }

    private EmployeeLoan CreateLoan(Employee employee, LoanType loanType, DateTime start, DateTime end, double due = 1000, double balance = 400)
    {
        EmployeeLoan loan = null;
        UsingDbContext(context =>
        {
            loan = context.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                LoanTypeId = loanType.Id,
                StartDate = start,
                EndDate = end,
                RecoveryStartDate = start,
                DueAmount = due,
                BalanceDueAmount = balance,
                FixedAmount = 100
            }).Entity;
        });
        return loan;
    }

    private static GetEmployeeLoanReportInput Input(DateTime from, DateTime to, string employeeCode = null, string loanTypeCode = null, int maxResultCount = 100)
    {
        return new GetEmployeeLoanReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            LoanTypeCode = loanTypeCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Filter_By_Loan_Payment_Date_Window()
    {
        var loanType = CreateLoanType(NewCode());
        var inRange = CreateEmployee("ELOAN-IN");
        var outRange = CreateEmployee("ELOAN-OUT");

        CreateLoan(inRange, loanType, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateLoan(outRange, loanType, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));

        var result = await _appService.GetEmployeeLoanReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("ELOAN-IN");
        codes.ShouldNotContain("ELOAN-OUT");
    }

    [Fact]
    public async Task Should_Compute_RecoveryAmount_As_Due_Minus_Balance()
    {
        var loanType = CreateLoanType(NewCode());
        var employee = CreateEmployee("ELOAN-CALC");
        CreateLoan(employee, loanType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), due: 1000, balance: 350);

        var result = await _appService.GetEmployeeLoanReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ELOAN-CALC"));

        result.Items.Single().RecoveryAmount.ShouldBe(650);
    }

    [Fact]
    public async Task Should_Resolve_OrgLevel_Columns_To_Empty_Strings_When_No_Org_Assignment_Exists()
    {
        var loanType = CreateLoanType(NewCode());
        var employee = CreateEmployee("ELOAN-ORG");
        CreateLoan(employee, loanType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeLoanReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ELOAN-ORG"));

        var row = result.Items.Single();
        row.OrgLevel1.ShouldBe(string.Empty);
        row.OrgLevel7.ShouldBe(string.Empty);
        row.JobTitleName.ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var loanType = CreateLoanType(NewCode());
        var a = CreateEmployee("ELOANEXP-A");
        var b = CreateEmployee("ELOANEXP-B");
        var outOfScope = CreateEmployee("ELOANEXP-OUT");

        CreateLoan(a, loanType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateLoan(b, loanType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateLoan(outOfScope, loanType, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetEmployeeLoanReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ELOANEXP-A,ELOANEXP-B", maxResultCount: 1));

        file.FileName.ShouldBe("EmployeeLoan.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
    }
}
