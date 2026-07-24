using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeAbsQuotaForfeitsAppService_Tests : AppTestBase
{
    private readonly IEmployeeAbsQuotaForfeitsAppService _appService;

    public EmployeeAbsQuotaForfeitsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAbsQuotaForfeitsAppService>();
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

    private AbsenceType CreateTestAbsenceType(string code)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return absenceType;
    }

    [Fact]
    public async Task Should_Create_Forfeit()
    {
        //Arrange
        var employee = CreateTestEmployee("EQF100");
        var absenceType = CreateTestAbsenceType("QFT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsQuotaForfeitDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            EffectiveDate = new DateTime(2024, 12, 31),
            QuotaForfeited = 4
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAbsQuotaForfeitsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaForfeited.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Delete_Forfeit()
    {
        //Arrange
        var employee = CreateTestEmployee("EQF200");
        var absenceType = CreateTestAbsenceType("QFT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsQuotaForfeitDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            EffectiveDate = new DateTime(2024, 12, 31),
            QuotaForfeited = 4
        });

        var created = await _appService.GetAll(new GetAllEmployeeAbsQuotaForfeitsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAbsQuotaForfeitsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }
}
