using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeLoansAppService_Tests : AppTestBase
{
    private readonly IEmployeeLoansAppService _appService;

    public EmployeeLoansAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeLoansAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        Employee employee = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        UsingDbContext(context =>
        {
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

    private LoanType CreateTestLoanType(string code)
    {
        LoanType loanType = null;
        UsingDbContext(context =>
        {
            loanType = context.Set<LoanType>().Add(new LoanType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return loanType;
    }

    [Fact]
    public async Task Should_Create_Loan_With_Initial_BalanceDueAmount()
    {
        //Arrange
        var employee = CreateTestEmployee("LN100");
        var loanType = CreateTestLoanType("LT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeLoanDto
        {
            EmployeeId = employee.Id,
            LoanTypeId = loanType.Id,
            StartDate = new DateTime(2024, 1, 1),
            RecoveryStartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 12, 31),
            ActualAmount = 5000,
            DueAmount = 5000
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeLoansInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].BalanceDueAmount.ShouldBe(5000);
    }

    [Fact]
    public async Task Should_Throw_When_ActualAmount_Less_Than_DueAmount()
    {
        //Arrange
        var employee = CreateTestEmployee("LN200");
        var loanType = CreateTestLoanType("LT200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeLoanDto
            {
                EmployeeId = employee.Id,
                LoanTypeId = loanType.Id,
                StartDate = new DateTime(2024, 1, 1),
                RecoveryStartDate = new DateTime(2024, 2, 1),
                EndDate = new DateTime(2024, 12, 31),
                ActualAmount = 1000,
                DueAmount = 5000
            }));
    }

    [Fact]
    public async Task Should_Throw_When_RecoveryStartDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("LN300");
        var loanType = CreateTestLoanType("LT300");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeLoanDto
            {
                EmployeeId = employee.Id,
                LoanTypeId = loanType.Id,
                StartDate = new DateTime(2024, 2, 1),
                RecoveryStartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                ActualAmount = 5000,
                DueAmount = 5000
            }));
    }
}
