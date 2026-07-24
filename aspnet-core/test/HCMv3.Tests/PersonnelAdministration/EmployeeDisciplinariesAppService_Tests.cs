using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeDisciplinariesAppService_Tests : AppTestBase
{
    private readonly IEmployeeDisciplinariesAppService _appService;

    public EmployeeDisciplinariesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDisciplinariesAppService>();
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

    private DisciplinaryType CreateTestDisciplinaryType(string code)
    {
        DisciplinaryType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DisciplinaryType>().Add(new DisciplinaryType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return type;
    }

    [Fact]
    public async Task Should_Create_EmployeeDisciplinary()
    {
        //Arrange
        var employee = CreateTestEmployee("ED100");
        var type = CreateTestDisciplinaryType("DT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDisciplinaryDto
        {
            EmployeeId = employee.Id,
            DisciplinaryTypeId = type.Id,
            DisciplinaryDate = new DateTime(2024, 3, 15),
            DisciplinaryTime = new DateTime(2024, 3, 15, 9, 0, 0),
            Severity = Severity.High
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDisciplinariesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].DisciplinaryTypeNameEn.ShouldBe("DT100");
    }
}
