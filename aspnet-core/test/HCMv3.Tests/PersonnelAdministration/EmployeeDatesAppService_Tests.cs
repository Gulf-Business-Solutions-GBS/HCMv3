using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeDatesAppService_Tests : AppTestBase
{
    private readonly IEmployeeDatesAppService _appService;

    public EmployeeDatesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDatesAppService>();
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

    private DateType CreateTestDateType(string code)
    {
        DateType dateType = null;
        UsingDbContext(context =>
        {
            dateType = context.Set<DateType>().Add(new DateType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return dateType;
    }

    [Fact]
    public async Task Should_Create_EmployeeDate()
    {
        //Arrange
        var employee = CreateTestEmployee("ED100");
        var dateType = CreateTestDateType("DT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDateDto
        {
            EmployeeId = employee.Id,
            DateTypeId = dateType.Id,
            DateData = new DateTime(2021, 5, 1),
            DayData = 1
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDatesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].DateTypeId.ShouldBe(dateType.Id);
    }

    [Fact]
    public async Task Should_Get_EmployeeDate_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("ED200");
        var dateType = CreateTestDateType("DT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDateDto
        {
            EmployeeId = employee.Id,
            DateTypeId = dateType.Id,
            DateData = new DateTime(2021, 5, 1),
            DayData = 2
        });

        var created = await _appService.GetAll(new GetAllEmployeeDatesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeDateForEdit(new EntityDto(id));

        //Assert
        result.EmployeeDate.ShouldNotBeNull();
        result.EmployeeDate.EmployeeId.ShouldBe(employee.Id);
        result.EmployeeDate.DayData.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Update_EmployeeDate()
    {
        //Arrange
        var employee = CreateTestEmployee("ED300");
        var dateType = CreateTestDateType("DT300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDateDto
        {
            EmployeeId = employee.Id,
            DateTypeId = dateType.Id,
            DateData = new DateTime(2021, 5, 1),
            DayData = 1
        });

        var created = await _appService.GetAll(new GetAllEmployeeDatesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDateDto
        {
            Id = id,
            EmployeeId = employee.Id,
            DateTypeId = dateType.Id,
            DateData = new DateTime(2022, 6, 1),
            DayData = 5
        });

        //Assert
        var result = await _appService.GetEmployeeDateForEdit(new EntityDto(id));
        result.EmployeeDate.DateData.ShouldBe(new DateTime(2022, 6, 1));
        result.EmployeeDate.DayData.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Delete_EmployeeDate()
    {
        //Arrange
        var employee = CreateTestEmployee("ED400");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDateDto
        {
            EmployeeId = employee.Id,
            DateData = new DateTime(2021, 5, 1),
            DayData = 1
        });

        var created = await _appService.GetAll(new GetAllEmployeeDatesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDatesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Get_EmployeeDates_Filtered_By_Employee()
    {
        //Arrange
        var employee1 = CreateTestEmployee("ED500");
        var employee2 = CreateTestEmployee("ED501");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDateDto
        {
            EmployeeId = employee1.Id,
            DateData = new DateTime(2021, 5, 1),
            DayData = 1
        });
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDateDto
        {
            EmployeeId = employee2.Id,
            DateData = new DateTime(2021, 6, 1),
            DayData = 2
        });

        //Act
        var result = await _appService.GetAll(new GetAllEmployeeDatesInput { EmployeeId = employee1.Id });

        //Assert
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee1.Id);
    }
}
