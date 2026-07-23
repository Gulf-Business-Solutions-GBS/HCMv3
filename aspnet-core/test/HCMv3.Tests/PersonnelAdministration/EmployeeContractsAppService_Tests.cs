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
public class EmployeeContractsAppService_Tests : AppTestBase
{
    private readonly IEmployeeContractsAppService _appService;

    public EmployeeContractsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeContractsAppService>();
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

    private ContractType CreateTestContractType(string code)
    {
        ContractType contractType = null;
        UsingDbContext(context =>
        {
            contractType = context.Set<ContractType>().Add(new ContractType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return contractType;
    }

    [Fact]
    public async Task Should_Create_Contract()
    {
        //Arrange
        var employee = CreateTestEmployee("C100");
        var contractType = CreateTestContractType("CT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeContractDto
        {
            EmployeeId = employee.Id,
            ContractTypeId = contractType.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2021, 1, 1)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeContractsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Contract_Period()
    {
        //Arrange
        var employee = CreateTestEmployee("C200");
        var contractType = CreateTestContractType("CT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeContractDto
        {
            EmployeeId = employee.Id,
            ContractTypeId = contractType.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31)
        });

        //Act & Assert - overlapping period for the same employee
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeContractDto
            {
                EmployeeId = employee.Id,
                ContractTypeId = contractType.Id,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2021, 6, 1)
            }));
    }
}
