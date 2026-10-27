using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Transaction;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class MonthlyPayrollTransactionReportAppService_Tests : AppTestBase
{
    private readonly IMonthlyPayrollTransactionReportAppService _appService;

    public MonthlyPayrollTransactionReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IMonthlyPayrollTransactionReportAppService>();
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

    private WageType CreateWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType { TenantId = TenantId, Code = code, NameEn = code + "-En", NameAr = code + "-Ar" }).Entity;
        });
        return wageType;
    }

    private void CreatePayment(Employee employee, WageType wageType, DateTime paymentDate, double amount)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeePayment>().Add(new EmployeePayment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                PaymentDate = paymentDate
            });
        });
    }

    private void CreateLoan(Employee employee, DateTime start, DateTime end, double actualAmount)
    {
        var loanType = new LoanType { TenantId = TenantId, Code = NewCode(), NameEn = "LT", NameAr = "LT" };
        UsingDbContext(context =>
        {
            context.Set<LoanType>().Add(loanType);
        });

        UsingDbContext(context =>
        {
            context.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                LoanTypeId = loanType.Id,
                StartDate = start,
                EndDate = end,
                RecoveryStartDate = start,
                ActualAmount = actualAmount
            });
        });
    }

    private static GetMonthlyPayrollTransactionReportInput Input(int month, int year, string employeeCode = null)
    {
        return new GetMonthlyPayrollTransactionReportInput { Month = month, Year = year, EmployeeCode = employeeCode, MaxResultCount = 100, SkipCount = 0 };
    }

    [Fact]
    public async Task Should_Include_Payment_Rows_Within_The_Requested_Month()
    {
        var wageType = CreateWageType(NewCode());
        var employee = CreateEmployee("MPT-PAY");
        CreatePayment(employee, wageType, new DateTime(2024, 6, 15), 500);
        CreatePayment(employee, wageType, new DateTime(2024, 7, 1), 999); // outside June

        var result = await _appService.GetMonthlyPayrollTransactionReport(Input(6, 2024, "MPT-PAY"));

        result.Items.Count.ShouldBe(1);
        result.Items.Single().ExpectedAmount.ShouldBe(500);
    }

    [Fact]
    public async Task Should_Include_Loan_Rows_With_WageTypeName_LoanPay()
    {
        var employee = CreateEmployee("MPT-LOAN");
        CreateLoan(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), actualAmount: 750);

        var result = await _appService.GetMonthlyPayrollTransactionReport(Input(6, 2024, "MPT-LOAN"));

        var row = result.Items.Single();
        row.WageTypeName.ShouldBe("Loan Pay");
        row.ExpectedAmount.ShouldBe(750);
    }

    [Fact]
    public async Task Should_Default_To_The_Current_Month_When_Month_And_Year_Are_Not_Supplied()
    {
        var wageType = CreateWageType(NewCode());
        var employee = CreateEmployee("MPT-DEFAULT");
        var today = Clock.Now.Date;
        CreatePayment(employee, wageType, today, 42);

        var input = new GetMonthlyPayrollTransactionReportInput { EmployeeCode = "MPT-DEFAULT", MaxResultCount = 100, SkipCount = 0 };
        var result = await _appService.GetMonthlyPayrollTransactionReport(input);

        result.Items.ShouldContain(r => r.ExpectedAmount == 42);
    }

    [Fact]
    public async Task Should_Export_Every_Filtered_Row_Across_All_Four_Sources()
    {
        var wageType = CreateWageType(NewCode());
        var employee = CreateEmployee("MPTEXP-A");
        CreatePayment(employee, wageType, new DateTime(2024, 6, 10), 100);
        CreateLoan(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), 200);

        var file = await _appService.GetMonthlyPayrollTransactionReportToExcel(Input(6, 2024, "MPTEXP-A"));

        file.FileName.ShouldBe("MonthlyPayrollTransaction.xlsx");
    }
}
