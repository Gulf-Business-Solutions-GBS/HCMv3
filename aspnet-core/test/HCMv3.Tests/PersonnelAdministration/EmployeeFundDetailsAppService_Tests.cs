using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeFundDetailsAppService_Tests : AppTestBase
{
    private readonly IEmployeeFundDetailsAppService _appService;
    private readonly IEmployeeFundsAppService _fundsAppService;

    public EmployeeFundDetailsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeFundDetailsAppService>();
        _fundsAppService = Resolve<IEmployeeFundsAppService>();
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

    private async Task<int> CreateTestFundAsync(int employeeId)
    {
        await _fundsAppService.CreateOrEdit(new CreateOrEditEmployeeFundDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        });

        var result = await _fundsAppService.GetAll(new GetAllEmployeeFundsInput { EmployeeId = employeeId });
        return result.Items[0].Id;
    }

    [Fact]
    public async Task Should_Create_FundDetail()
    {
        //Arrange
        var employee = CreateTestEmployee("FD100");
        var fundId = await CreateTestFundAsync(employee.Id);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFundDetailDto
        {
            EmployeeFundId = fundId,
            TransactionDate = new DateTime(2024, 1, 31),
            Amount = 100,
            FundPayType = FundPayType.Deduction
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeFundDetailsInput { EmployeeFundId = fundId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].FundPayType.ShouldBe(FundPayType.Deduction);
    }
}
