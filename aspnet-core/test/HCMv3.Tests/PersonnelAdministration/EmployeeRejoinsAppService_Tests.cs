using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeRejoinsAppService_Tests : AppTestBase
{
    private readonly IEmployeeRejoinsAppService _appService;

    public EmployeeRejoinsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeRejoinsAppService>();
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

    [Fact]
    public async Task Should_Create_Rejoin_As_Record_Keeping_Only()
    {
        //Arrange
        var employee = CreateTestEmployee("R100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRejoinDto
        {
            EmployeeId = employee.Id,
            RejoinDate = new DateTime(2023, 1, 1),
            CarryForwardLeaveBalance = true,
            CarryForwardTicketBalance = false,
            CarryForwardIndemnityBalance = true,
            CarryForwardLoanBalance = false
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeRejoinsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CarryForwardLeaveBalance.ShouldBeTrue();
        result.Items[0].CarryForwardTicketBalance.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Throw_When_Rejoin_Not_Found_For_Edit()
    {
        //Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _appService.GetEmployeeRejoinForEdit(new EntityDto(99999)));
    }
}
