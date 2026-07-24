using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeVisaInfosAppService_Tests : AppTestBase
{
    private readonly IEmployeeVisaInfosAppService _appService;

    public EmployeeVisaInfosAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeVisaInfosAppService>();
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

    private PassportType CreateTestPassportType(string code)
    {
        PassportType passportType = null;
        UsingDbContext(context =>
        {
            passportType = context.Set<PassportType>().Add(new PassportType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return passportType;
    }

    [Fact]
    public async Task Should_Create_EmployeeVisaInfo()
    {
        //Arrange
        var employee = CreateTestEmployee("V100");
        var passportType = CreateTestPassportType("PT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeVisaInfoDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2022, 1, 1),
            VisaNum = 12345,
            ResidencyNum = 67890,
            PassportTypeId = passportType.Id
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeVisaInfosInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].VisaNum.ShouldBe(12345);
    }

    [Fact]
    public async Task Should_Get_EmployeeVisaInfo_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("V200");
        var passportType = CreateTestPassportType("PT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeVisaInfoDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2022, 1, 1),
            VisaNum = 22222,
            ResidencyNum = 33333,
            PassportTypeId = passportType.Id
        });

        var created = await _appService.GetAll(new GetAllEmployeeVisaInfosInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeVisaInfoForEdit(new EntityDto(id));

        //Assert
        result.EmployeeVisaInfo.ShouldNotBeNull();
        result.EmployeeVisaInfo.VisaNum.ShouldBe(22222);
    }

    [Fact]
    public async Task Should_Update_EmployeeVisaInfo()
    {
        //Arrange
        var employee = CreateTestEmployee("V300");
        var passportType = CreateTestPassportType("PT300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeVisaInfoDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2022, 1, 1),
            VisaNum = 11111,
            ResidencyNum = 22222,
            PassportTypeId = passportType.Id
        });

        var created = await _appService.GetAll(new GetAllEmployeeVisaInfosInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeVisaInfoDto
        {
            Id = id,
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2022, 1, 1),
            VisaNum = 99999,
            ResidencyNum = 22222,
            PassportTypeId = passportType.Id
        });

        //Assert
        var result = await _appService.GetEmployeeVisaInfoForEdit(new EntityDto(id));
        result.EmployeeVisaInfo.VisaNum.ShouldBe(99999);
    }

    [Fact]
    public async Task Should_Delete_EmployeeVisaInfo()
    {
        //Arrange
        var employee = CreateTestEmployee("V400");
        var passportType = CreateTestPassportType("PT400");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeVisaInfoDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2022, 1, 1),
            VisaNum = 44444,
            ResidencyNum = 55555,
            PassportTypeId = passportType.Id
        });

        var created = await _appService.GetAll(new GetAllEmployeeVisaInfosInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeVisaInfosInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_On_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("V500");
        var passportType = CreateTestPassportType("PT500");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeVisaInfoDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2022, 1, 1),
                EndDate = new DateTime(2020, 1, 1),
                VisaNum = 1,
                ResidencyNum = 1,
                PassportTypeId = passportType.Id
            }));
    }

    [Fact]
    public async Task Should_Get_EmployeeVisaInfos_Filtered_By_Employee()
    {
        //Arrange
        var employee1 = CreateTestEmployee("V600");
        var employee2 = CreateTestEmployee("V601");
        var passportType = CreateTestPassportType("PT600");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeVisaInfoDto
        {
            EmployeeId = employee1.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2022, 1, 1),
            VisaNum = 1,
            ResidencyNum = 1,
            PassportTypeId = passportType.Id
        });
        await _appService.CreateOrEdit(new CreateOrEditEmployeeVisaInfoDto
        {
            EmployeeId = employee2.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2022, 1, 1),
            VisaNum = 2,
            ResidencyNum = 2,
            PassportTypeId = passportType.Id
        });

        //Act
        var result = await _appService.GetAll(new GetAllEmployeeVisaInfosInput { EmployeeId = employee1.Id });

        //Assert
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee1.Id);
    }
}
