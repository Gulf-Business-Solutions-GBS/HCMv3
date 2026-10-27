using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeLoanExemptionsAppService_Tests : AppTestBase
{
    private readonly IEmployeeLoanExemptionsAppService _appService;
    private readonly IEmployeeLoansAppService _loansAppService;

    public EmployeeLoanExemptionsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeLoanExemptionsAppService>();
        _loansAppService = Resolve<IEmployeeLoansAppService>();
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

    private async Task<int> CreateTestLoanAsync(int employeeId, int loanTypeId)
    {
        await _loansAppService.CreateOrEdit(new CreateOrEditEmployeeLoanDto
        {
            EmployeeId = employeeId,
            LoanTypeId = loanTypeId,
            StartDate = new DateTime(2024, 1, 1),
            RecoveryStartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 12, 31),
            ActualAmount = 5000,
            DueAmount = 5000
        });

        var result = await _loansAppService.GetAll(new GetAllEmployeeLoansInput { EmployeeId = employeeId });
        return result.Items[0].Id;
    }

    [Fact]
    public async Task Should_Create_Exemption()
    {
        //Arrange
        var employee = CreateTestEmployee("LE100");
        var loanType = CreateTestLoanType("LTE100");
        var loanId = await CreateTestLoanAsync(employee.Id, loanType.Id);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeLoanExemptionDto
        {
            EmployeeLoanId = loanId,
            ExemptionDate = new DateTime(2024, 3, 1)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeLoanExemptionsInput { EmployeeLoanId = loanId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Month_Exemption()
    {
        //Arrange
        var employee = CreateTestEmployee("LE200");
        var loanType = CreateTestLoanType("LTE200");
        var loanId = await CreateTestLoanAsync(employee.Id, loanType.Id);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeLoanExemptionDto
        {
            EmployeeLoanId = loanId,
            ExemptionDate = new DateTime(2024, 3, 5)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeLoanExemptionDto
            {
                EmployeeLoanId = loanId,
                ExemptionDate = new DateTime(2024, 3, 20)
            }));
    }
}
