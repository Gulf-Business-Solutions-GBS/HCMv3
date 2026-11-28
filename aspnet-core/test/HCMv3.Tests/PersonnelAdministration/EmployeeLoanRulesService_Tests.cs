using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The legacy (LHRMS) loan rules run through <c>IEmployeeLoanRulesService</c>: the loan type's eligibility limits on
/// a loan, its recovery option and instalment limit on a recovery, and the loan's balance due kept in step with its
/// recoveries. Exercised through the loan and recovery application services, which is where they apply.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeLoanRulesService_Tests : AppTestBase
{
    private readonly IEmployeeLoansAppService _loans;
    private readonly IEmployeeLoanRecoveriesAppService _recoveries;

    public EmployeeLoanRulesService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _loans = Resolve<IEmployeeLoansAppService>();
        _recoveries = Resolve<IEmployeeLoanRecoveriesAppService>();
    }

    // ------------------------------------------------------------------ loan

    [Fact]
    public async Task Should_Refuse_A_Loan_With_No_Due_Amount()
    {
        // Arrange
        var employee = CreateEmployee("LR001");
        var loanType = CreateLoanType("LRT001");
        var loan = NewLoan(employee.Id, loanType.Id);
        loan.DueAmount = 0;

        // Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() => _loans.CreateOrEdit(loan));
    }

    [Fact]
    public async Task Should_Refuse_A_Loan_Before_The_Eligible_Period()
    {
        // Arrange: joined 2020-01-01, the type needs 12 months of service.
        var employee = CreateEmployee("LR002");
        var loanType = CreateLoanType("LRT002", t => t.EligiblePeriod = 12);
        var loan = NewLoan(employee.Id, loanType.Id, new DateTime(2020, 6, 1));

        // Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() => _loans.CreateOrEdit(loan));
    }

    [Fact]
    public async Task Should_Allow_A_Loan_Once_The_Eligible_Period_Has_Passed()
    {
        // Arrange
        var employee = CreateEmployee("LR003");
        var loanType = CreateLoanType("LRT003", t => t.EligiblePeriod = 12);

        // Act
        var id = await _loans.CreateOrEdit(NewLoan(employee.Id, loanType.Id, new DateTime(2021, 1, 1)));

        // Assert
        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Refuse_More_Loans_In_A_Year_Than_The_Type_Allows()
    {
        // Arrange: one loan of the type a year, and the employee already has one this year.
        var employee = CreateEmployee("LR004");
        var loanType = CreateLoanType("LRT004", t => t.EligibleCountPerYear = 1);
        await _loans.CreateOrEdit(NewLoan(employee.Id, loanType.Id));

        // Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() => _loans.CreateOrEdit(NewLoan(employee.Id, loanType.Id)));
    }

    [Fact]
    public async Task Should_Not_Count_The_Loan_Being_Edited_Against_The_Yearly_Limit()
    {
        // Arrange
        var employee = CreateEmployee("LR005");
        var loanType = CreateLoanType("LRT005", t => t.EligibleCountPerYear = 1);
        var loan = NewLoan(employee.Id, loanType.Id);
        loan.Id = await _loans.CreateOrEdit(loan);
        loan.Comments = "Edited";

        // Act / Assert
        await Should.NotThrowAsync(() => _loans.CreateOrEdit(loan));
    }

    [Fact]
    public async Task Should_Refuse_A_Due_Amount_Above_The_Eligible_Amount()
    {
        // Arrange: basic salary 1000, the type lends up to 2 x basic = 2000; 5000 is asked for.
        var employee = CreateEmployee("LR006");
        CreateBasicSalary(employee.Id, 1000);
        var loanType = CreateLoanType("LRT006", t =>
        {
            t.EligibleAmount = EligibleAmount.BasicSalary;
            t.EligibleAmountNumber = 2;
        });

        // Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() => _loans.CreateOrEdit(NewLoan(employee.Id, loanType.Id)));
    }

    [Fact]
    public async Task Should_Allow_A_Due_Amount_Within_The_Eligible_Amount()
    {
        // Arrange: 10 x basic 1000 = 10000 covers 5000.
        var employee = CreateEmployee("LR007");
        CreateBasicSalary(employee.Id, 1000);
        var loanType = CreateLoanType("LRT007", t =>
        {
            t.EligibleAmount = EligibleAmount.BasicSalary;
            t.EligibleAmountNumber = 10;
        });

        // Act
        var id = await _loans.CreateOrEdit(NewLoan(employee.Id, loanType.Id));

        // Assert
        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Recalculate_Balance_Due_When_The_Due_Amount_Is_Edited()
    {
        // Arrange: 5000 due, 1000 recovered.
        var employee = CreateEmployee("LR008");
        var loanType = CreateLoanType("LRT008");
        var loan = NewLoan(employee.Id, loanType.Id);
        loan.Id = await _loans.CreateOrEdit(loan);
        await _recoveries.CreateOrEdit(NewRecovery(loan.Id.Value, 1000));

        // Act
        loan.DueAmount = 4500;
        await _loans.CreateOrEdit(loan);

        // Assert
        (await BalanceDueAsync(employee.Id)).ShouldBe(3500);
    }

    // ------------------------------------------------------------------ recovery

    [Fact]
    public async Task Should_Lower_Balance_Due_When_A_Recovery_Is_Added()
    {
        // Arrange
        var employee = CreateEmployee("LR009");
        var loanId = await _loans.CreateOrEdit(NewLoan(employee.Id, CreateLoanType("LRT009").Id));

        // Act
        await _recoveries.CreateOrEdit(NewRecovery(loanId, 1200));

        // Assert
        (await BalanceDueAsync(employee.Id)).ShouldBe(3800);
    }

    [Fact]
    public async Task Should_Restore_Balance_Due_When_A_Recovery_Is_Deleted()
    {
        // Arrange
        var employee = CreateEmployee("LR010");
        var loanId = await _loans.CreateOrEdit(NewLoan(employee.Id, CreateLoanType("LRT010").Id));
        await _recoveries.CreateOrEdit(NewRecovery(loanId, 1200));
        var recovery = (await _recoveries.GetAll(new GetAllEmployeeLoanRecoveriesInput { EmployeeLoanId = loanId })).Items.Single();

        // Act
        await _recoveries.Delete(new EntityDto(recovery.Id));

        // Assert
        (await BalanceDueAsync(employee.Id)).ShouldBe(5000);
    }

    [Fact]
    public async Task Should_Refuse_A_Payroll_Recovery_When_The_Type_Recovers_Externally_Only()
    {
        // Arrange
        var employee = CreateEmployee("LR011");
        var loanType = CreateLoanType("LRT011", t => t.RecoveryOption = RecoveryOption.ThroughExternal);
        var loanId = await _loans.CreateOrEdit(NewLoan(employee.Id, loanType.Id));

        // Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _recoveries.CreateOrEdit(NewRecovery(loanId, 100, LoanRecoveryMethod.RecoveryThroughPayroll)));
    }

    [Fact]
    public async Task Should_Refuse_An_External_Recovery_When_The_Type_Recovers_Through_Payroll_Only()
    {
        // Arrange
        var employee = CreateEmployee("LR012");
        var loanType = CreateLoanType("LRT012", t => t.RecoveryOption = RecoveryOption.ThroughPayroll);
        var loanId = await _loans.CreateOrEdit(NewLoan(employee.Id, loanType.Id));

        // Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _recoveries.CreateOrEdit(NewRecovery(loanId, 100, LoanRecoveryMethod.RecoveryExternal)));
    }

    [Fact]
    public async Task Should_Refuse_More_Recoveries_Than_The_Max_Installments()
    {
        // Arrange: the type allows 2 instalments and the loan has had 2.
        var employee = CreateEmployee("LR013");
        var loanType = CreateLoanType("LRT013", t => t.MaxInstallments = 2);
        var loanId = await _loans.CreateOrEdit(NewLoan(employee.Id, loanType.Id));
        await _recoveries.CreateOrEdit(NewRecovery(loanId, 100));
        await _recoveries.CreateOrEdit(NewRecovery(loanId, 100));

        // Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() => _recoveries.CreateOrEdit(NewRecovery(loanId, 100)));
    }

    // ------------------------------------------------------------------ helpers

    private static CreateOrEditEmployeeLoanDto NewLoan(int employeeId, int loanTypeId, DateTime? startDate = null)
    {
        var start = startDate ?? new DateTime(2024, 1, 1);
        return new CreateOrEditEmployeeLoanDto
        {
            EmployeeId = employeeId,
            LoanTypeId = loanTypeId,
            StartDate = start,
            RecoveryStartDate = start.AddMonths(1),
            EndDate = start.AddMonths(11),
            ActualAmount = 5000,
            DueAmount = 5000
        };
    }

    private static CreateOrEditEmployeeLoanRecoveryDto NewRecovery(
        int loanId,
        double amount,
        LoanRecoveryMethod method = LoanRecoveryMethod.RecoveryExternal) => new()
    {
        EmployeeLoanId = loanId,
        RecoveryDate = new DateTime(2024, 3, 1),
        RecoveryAmount = amount,
        LoanRecoveryMethod = method
    };

    private async Task<double> BalanceDueAsync(int employeeId)
    {
        var result = await _loans.GetAll(new GetAllEmployeeLoansInput { EmployeeId = employeeId });
        return result.Items.Single().BalanceDueAmount;
    }

    private Employee CreateEmployee(string code)
    {
        Employee employee = null;
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private LoanType CreateLoanType(string code, Action<LoanType> configure = null)
    {
        LoanType loanType = null;
        UsingDbContext(context =>
        {
            var entity = new LoanType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            };
            configure?.Invoke(entity);
            loanType = context.Set<LoanType>().Add(entity).Entity;
        });
        return loanType;
    }

    /// <summary>A basic pay in force from 2019 with one line on wage type 1000 (basic salary).</summary>
    private void CreateBasicSalary(int employeeId, double amount)
    {
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = tenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Kuwaiti Dinar",
                NameAr = "دينار كويتي",
                DecimalCount = 3
            }).Entity;
            var wageType = context.Set<WageType>().FirstOrDefault(w => w.TenantId == tenantId && w.Code == "1000")
                ?? context.Set<WageType>().Add(new WageType
                {
                    TenantId = tenantId,
                    Code = "1000",
                    NameEn = "Basic Salary",
                    NameAr = "الراتب الأساسي",
                    BillingType = BillingType.Earning,
                    IncludeInBasicPay = true
                }).Entity;
            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2019, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                Amount = amount,
                PayType = PayType.Regular,
                PayInterval = PayInterval.Monthly
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
            {
                TenantId = tenantId,
                EmployeeBasicPayId = basicPay.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                CurrencyId = currency.Id
            });
        });
    }
}
