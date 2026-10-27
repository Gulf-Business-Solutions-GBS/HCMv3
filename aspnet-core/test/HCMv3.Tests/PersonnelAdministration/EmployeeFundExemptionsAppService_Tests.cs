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
public class EmployeeFundExemptionsAppService_Tests : AppTestBase
{
    private readonly IEmployeeFundExemptionsAppService _appService;
    private readonly IEmployeeFundsAppService _fundsAppService;

    public EmployeeFundExemptionsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeFundExemptionsAppService>();
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
    public async Task Should_Create_FundExemption()
    {
        //Arrange
        var employee = CreateTestEmployee("FE100");
        var fundId = await CreateTestFundAsync(employee.Id);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFundExemptionDto
        {
            EmployeeFundId = fundId,
            StartDate = new DateTime(2024, 3, 1),
            EndDate = new DateTime(2024, 3, 31)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeFundExemptionsInput { EmployeeFundId = fundId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("FE200");
        var fundId = await CreateTestFundAsync(employee.Id);

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeFundExemptionDto
            {
                EmployeeFundId = fundId,
                StartDate = new DateTime(2024, 3, 31),
                EndDate = new DateTime(2024, 3, 1)
            }));
    }
}
