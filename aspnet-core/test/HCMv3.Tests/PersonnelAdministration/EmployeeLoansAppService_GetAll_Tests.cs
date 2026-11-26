using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The Employee Loans list screen (every employee's loans, legacy "Loan" menu item) searches by employee and
/// loan type.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeLoansAppService_GetAll_Tests : AppTestBase
{
    private readonly IEmployeeLoansAppService _appService;

    public EmployeeLoansAppService_GetAll_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeLoansAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code, string nameEn, string nameAr)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = nameEn,
                FullNameAr = nameAr,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
            context.SaveChanges();
            id = employee.Id;
        });
        return id;
    }

    private int CreateLoanType(string code, string nameEn)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var loanType = context.Set<LoanType>().Add(new LoanType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
            context.SaveChanges();
            id = loanType.Id;
        });
        return id;
    }

    private Task AddLoanAsync(int employeeId, int loanTypeId)
    {
        return _appService.CreateOrEdit(new CreateOrEditEmployeeLoanDto
        {
            EmployeeId = employeeId,
            LoanTypeId = loanTypeId,
            StartDate = new DateTime(2024, 1, 1),
            RecoveryStartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 12, 31),
            ActualAmount = 1000,
            DueAmount = 1000
        });
    }

    /// <summary>Two employees, each with a loan of a different type.</summary>
    private async Task<(int Ahmad, int Sara)> ArrangeTwoLoansAsync()
    {
        var ahmad = CreateEmployee("LNF01", "Ahmad Salem", "أحمد سالم");
        var sara = CreateEmployee("LNF02", "Sara Khalid", "سارة خالد");
        var carLoan = CreateLoanType("LTF01", "Car Loan");
        var housingLoan = CreateLoanType("LTF02", "Housing Loan");

        await AddLoanAsync(ahmad, carLoan);
        await AddLoanAsync(sara, housingLoan);

        return (ahmad, sara);
    }

    [Fact]
    public async Task Should_List_Every_Employees_Loans_Without_A_Filter()
    {
        var (ahmad, sara) = await ArrangeTwoLoansAsync();

        var result = await _appService.GetAll(new GetAllEmployeeLoansInput { MaxResultCount = 100 });

        result.Items.Select(i => i.EmployeeId).ShouldContain(ahmad);
        result.Items.Select(i => i.EmployeeId).ShouldContain(sara);
    }

    [Theory]
    [InlineData("LNF01")]
    [InlineData("Ahmad")]
    [InlineData("أحمد")]
    public async Task Should_Find_Loans_By_Employee_Code_Or_Name(string filter)
    {
        var (ahmad, _) = await ArrangeTwoLoansAsync();

        var result = await _appService.GetAll(new GetAllEmployeeLoansInput { Filter = filter });

        result.Items.ShouldAllBe(i => i.EmployeeId == ahmad);
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Find_Loans_By_Loan_Type_Name()
    {
        var (_, sara) = await ArrangeTwoLoansAsync();

        var result = await _appService.GetAll(new GetAllEmployeeLoansInput { Filter = "Housing" });

        result.Items.ShouldAllBe(i => i.EmployeeId == sara);
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Apply_The_Filter_Within_One_Employee()
    {
        var (ahmad, _) = await ArrangeTwoLoansAsync();

        var result = await _appService.GetAll(new GetAllEmployeeLoansInput { EmployeeId = ahmad, Filter = "Housing" });

        result.TotalCount.ShouldBe(0);
    }
}
