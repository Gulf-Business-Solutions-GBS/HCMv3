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
public class EmployeeRemunerationsAppService_Tests : AppTestBase
{
    private readonly IEmployeeRemunerationsAppService _appService;

    public EmployeeRemunerationsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeRemunerationsAppService>();
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
    public async Task Should_Create_EmployeeRemuneration()
    {
        //Arrange
        var employee = CreateTestEmployee("ER100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRemunerationDto
        {
            EmployeeId = employee.Id,
            PaymentDate = new DateTime(2024, 3, 15),
            PaymentNumber = 3,
            PaymentUnit = PaymentUnit.Days
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeRemunerationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_PaymentNumber_Is_Zero()
    {
        //Arrange
        var employee = CreateTestEmployee("ER200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeRemunerationDto
            {
                EmployeeId = employee.Id,
                PaymentDate = new DateTime(2024, 3, 15),
                PaymentNumber = 0
            }));
    }

    [Fact]
    public async Task Should_Show_LHRMS_Message_When_PaymentNumber_Is_Zero()
    {
        //Arrange
        var employee = CreateTestEmployee("ER300");

        //Act
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeRemunerationDto
            {
                EmployeeId = employee.Id,
                PaymentDate = new DateTime(2024, 3, 15),
                PaymentNumber = 0,
                PaymentUnit = PaymentUnit.Hours
            }));

        //Assert
        ex.Message.ShouldBe("Payment Number cannot be blank or zero.");
    }

    [Fact]
    public async Task Should_Return_Employee_Code_And_Name_Sorted_By_Employee_Code()
    {
        //Arrange
        var employee = CreateTestEmployee("ER400");
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRemunerationDto
        {
            EmployeeId = employee.Id,
            PaymentDate = new DateTime(2024, 3, 15),
            PaymentNumber = 6,
            PaymentUnit = PaymentUnit.Hours,
            Comments = "Half day"
        });

        //Act
        var result = await _appService.GetAll(new GetAllEmployeeRemunerationsInput
        {
            EmployeeId = employee.Id,
            Sorting = "employee.code ASC"
        });

        //Assert
        var row = result.Items.ShouldHaveSingleItem();
        row.EmployeeCode.ShouldBe("ER400");
        row.EmployeeFullNameEn.ShouldBe(employee.FullNameEn);
        row.PaymentUnit.ShouldBe(PaymentUnit.Hours);
        row.PaymentNumber.ShouldBe(6);
        row.Comments.ShouldBe("Half day");
    }

    [Fact]
    public async Task Should_Keep_Hidden_WageType_When_Edited()
    {
        //Arrange
        var employee = CreateTestEmployee("ER500");
        var wageTypeId = UsingDbContext(context =>
        {
            var wageType = context.WageTypes.Add(new HCMv3.PayrollManagement.WageType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "1012",
                NameEn = "Incentive",
                NameAr = "Incentive"
            }).Entity;
            context.SaveChanges();
            return wageType.Id;
        });
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRemunerationDto
        {
            EmployeeId = employee.Id,
            WageTypeId = wageTypeId,
            PaymentDate = new DateTime(2024, 3, 15),
            PaymentNumber = 2,
            PaymentUnit = PaymentUnit.Days
        });
        var id = (await _appService.GetAll(new GetAllEmployeeRemunerationsInput { EmployeeId = employee.Id })).Items[0].Id;

        //Act -- the screen round-trips the loaded DTO, so the hidden WageTypeId comes back unchanged.
        var edit = (await _appService.GetEmployeeRemunerationForEdit(new Abp.Application.Services.Dto.EntityDto(id))).EmployeeRemuneration;
        edit.PaymentNumber = 4;
        await _appService.CreateOrEdit(edit);

        //Assert
        var saved = (await _appService.GetEmployeeRemunerationForEdit(new Abp.Application.Services.Dto.EntityDto(id))).EmployeeRemuneration;
        saved.WageTypeId.ShouldBe(wageTypeId);
        saved.PaymentNumber.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Delete_EmployeeRemuneration()
    {
        //Arrange
        var employee = CreateTestEmployee("ER600");
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRemunerationDto
        {
            EmployeeId = employee.Id,
            PaymentDate = new DateTime(2024, 3, 15),
            PaymentNumber = 1,
            PaymentUnit = PaymentUnit.Days
        });
        var id = (await _appService.GetAll(new GetAllEmployeeRemunerationsInput { EmployeeId = employee.Id })).Items[0].Id;

        //Act
        await _appService.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        //Assert
        (await _appService.GetAll(new GetAllEmployeeRemunerationsInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(0);
    }
}
