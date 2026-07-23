using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeesAppService_Tests : AppTestBase
{
    private readonly IEmployeesAppService _employeesAppService;

    public EmployeesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _employeesAppService = Resolve<IEmployeesAppService>();
    }

    private Nationality CreateTestNationality(string nameEn = "Test Nationality")
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
        });
        return nationality;
    }

    private CreateOrEditEmployeeDto ValidCreateDto(int nationalityId, string code = null)
    {
        return new CreateOrEditEmployeeDto
        {
            Code = code,
            FirstNameEn = "John",
            LastNameEn = "Doe",
            FirstNameAr = "جون",
            LastNameAr = "دو",
            BirthDate = new DateTime(1990, 1, 1),
            JoiningDate = new DateTime(2020, 1, 1),
            ProbationDays = 90,
            NationalityId = nationalityId,
            CivilID = 123456789012
        };
    }

    [Fact]
    public async Task Should_Create_Employee_And_Compute_FullName_And_ProbationEndDate()
    {
        //Arrange
        var nationality = CreateTestNationality();

        //Act
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "E100"));

        //Assert
        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().Single(e => e.Code == "E100");
            employee.FullNameEn.ShouldBe("John Doe");
            employee.FullNameAr.ShouldBe("جون دو");
            employee.ProbationEndDate.ShouldBe(employee.JoiningDate.AddDays(90));
        });
    }

    [Fact]
    public async Task Should_Throw_When_BirthDate_Is_Future_Or_Current()
    {
        //Arrange
        var nationality = CreateTestNationality();
        var dto = ValidCreateDto(nationality.Id, "E101");
        dto.BirthDate = DateTime.Now.Date;

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeesAppService.CreateOrEdit(dto));
    }

    [Fact]
    public async Task Should_Throw_When_JoiningDate_Earlier_Than_BirthDate()
    {
        //Arrange
        var nationality = CreateTestNationality();
        var dto = ValidCreateDto(nationality.Id, "E102");
        dto.JoiningDate = new DateTime(1980, 1, 1);

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeesAppService.CreateOrEdit(dto));
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Active_CivilId()
    {
        //Arrange
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "E103"));

        var dto = ValidCreateDto(nationality.Id, "E104");
        dto.CivilID = 123456789012; // same as E103

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeesAppService.CreateOrEdit(dto));
    }

    [Fact]
    public async Task Should_Auto_Generate_Code_When_Blank()
    {
        //Arrange
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "5"));

        var dto = ValidCreateDto(nationality.Id);
        dto.CivilID = 999888777666;

        //Act
        await _employeesAppService.CreateOrEdit(dto);

        //Assert
        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().Single(e => e.CivilID == 999888777666);
            employee.Code.ShouldBe("6");
        });
    }

    [Fact]
    public async Task Should_Get_Employee_For_Edit()
    {
        //Arrange
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "E105"));
        int id = 0;
        UsingDbContext(context => id = context.Set<Employee>().Single(e => e.Code == "E105").Id);

        //Act
        var output = await _employeesAppService.GetEmployeeForEdit(new EntityDto(id));

        //Assert
        output.Employee.ShouldNotBeNull();
        output.Employee.Code.ShouldBe("E105");
    }

    [Fact]
    public async Task Should_Delete_Employee()
    {
        //Arrange
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "E106"));
        int id = 0;
        UsingDbContext(context => id = context.Set<Employee>().Single(e => e.Code == "E106").Id);

        //Act
        await _employeesAppService.Delete(new EntityDto(id));

        //Assert
        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().IgnoreQueryFilters().Single(e => e.Id == id);
            employee.IsDeleted.ShouldBeTrue();
        });
    }
}
