using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeFnFPaymentsAppService_Tests : AppTestBase
{
    private readonly IEmployeeFnFPaymentsAppService _appService;

    public EmployeeFnFPaymentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeFnFPaymentsAppService>();
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
    public async Task Should_Create_FnFPayment()
    {
        //Arrange
        var employee = CreateTestEmployee("EFF100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFnFPaymentDto
        {
            EmployeeId = employee.Id,
            CompensationDate = new DateTime(2024, 6, 30),
            LeaveCompensateDays = 10,
            GratuityCompensateDays = 45,
            IndemnityCompensateAmount = 2000,
            CompensateBonusPay = 500
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeFnFPaymentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].IndemnityCompensateAmount.ShouldBe(2000);
    }

    [Fact]
    public async Task Should_Update_FnFPayment()
    {
        //Arrange
        var employee = CreateTestEmployee("EFF200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeFnFPaymentDto
        {
            EmployeeId = employee.Id,
            CompensationDate = new DateTime(2024, 6, 30),
            LeaveCompensateDays = 10
        });

        var created = await _appService.GetAll(new GetAllEmployeeFnFPaymentsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeFnFPaymentDto
        {
            Id = id,
            EmployeeId = employee.Id,
            CompensationDate = new DateTime(2024, 6, 30),
            LeaveCompensateDays = 20
        });

        //Assert
        var updated = await _appService.GetEmployeeFnFPaymentForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        updated.EmployeeFnFPayment.LeaveCompensateDays.ShouldBe(20);
    }
}
