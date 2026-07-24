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
public class EmployeeBanksAppService_Tests : AppTestBase
{
    private readonly IEmployeeBanksAppService _appService;

    public EmployeeBanksAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeBanksAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
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

        Employee employee = null;
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

    private Bank CreateTestBank(string code)
    {
        Bank bank = null;
        UsingDbContext(context =>
        {
            bank = context.Set<Bank>().Add(new Bank
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return bank;
    }

    [Fact]
    public async Task Should_Create_EmployeeBank()
    {
        //Arrange
        var employee = CreateTestEmployee("EB100");
        var bank = CreateTestBank("BK100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBankDto
        {
            EmployeeId = employee.Id,
            BankId = bank.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            PaymentType = PaymentType.Bank
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeBanksInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].BankNameEn.ShouldBe("BK100");
    }

    [Fact]
    public async Task Should_Throw_When_Bank_Missing_For_Bank_PaymentType()
    {
        //Arrange
        var employee = CreateTestEmployee("EB200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeBankDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                PaymentType = PaymentType.Bank
            }));
    }
}
