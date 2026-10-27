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
public class EmployeeBenefitExceptionsAppService_Tests : AppTestBase
{
    private readonly IEmployeeBenefitExceptionsAppService _appService;

    public EmployeeBenefitExceptionsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeBenefitExceptionsAppService>();
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

    [Fact]
    public async Task Should_Create_EmployeeBenefitException()
    {
        //Arrange
        var employee = CreateTestEmployee("EBE100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBenefitExceptionDto
        {
            EmployeeId = employee.Id,
            ExceptionBenefitType = ExceptionBenefitType.HealthReimbursment,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            Amount = 500
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeBenefitExceptionsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Overlapping_ExceptionBenefitType()
    {
        //Arrange
        var employee = CreateTestEmployee("EBE200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeBenefitExceptionDto
        {
            EmployeeId = employee.Id,
            ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            Amount = 200
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeBenefitExceptionDto
            {
                EmployeeId = employee.Id,
                ExceptionBenefitType = ExceptionBenefitType.Ticket,
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(9999, 12, 31),
                Amount = 300
            }));
    }
}
