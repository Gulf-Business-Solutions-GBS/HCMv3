using System;
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

// ReSharper disable once InconsistentNaming
public class EmployeeBasicPaysAppService_Tests : AppTestBase
{
    private readonly IEmployeeBasicPaysAppService _appService;

    public EmployeeBasicPaysAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeBasicPaysAppService>();
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
    public async Task Should_Create_BasicPay()
    {
        //Arrange
        var employee = CreateTestEmployee("BP100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1),
            PayType = PayType.Regular,
            PayInterval = PayInterval.Monthly
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EndDate.Year.ShouldBe(9999);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("BP200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2024, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Delete_BasicPay()
    {
        //Arrange
        var employee = CreateTestEmployee("BP300");
        await _appService.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }
}
