using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeAbsenceQuotasAppService_Tests : AppTestBase
{
    private readonly IEmployeeAbsenceQuotasAppService _appService;

    public EmployeeAbsenceQuotasAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAbsenceQuotasAppService>();
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
    public async Task Should_Create_Quota_And_Compute_Balance()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ100");
        var absenceType = CreateTestAbsenceType("AQT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceQuotaDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            EntitlementFrom = new DateTime(2024, 1, 1),
            EntitlementTo = new DateTime(9999, 12, 31),
            DeductionFrom = new DateTime(2024, 1, 1),
            DeductionTo = new DateTime(9999, 12, 31),
            QuotaAccrued = 21,
            QuotaUsed = 5,
            QuotaAdjusted = 1,
            QuotaForfeited = 0
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAbsenceQuotasInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaBalance.ShouldBe(15);
    }

    [Fact]
    public async Task Should_Throw_When_EntitlementTo_Earlier_Than_EntitlementFrom()
    {
        //Arrange
        var employee = CreateTestEmployee("EQ200");
        var absenceType = CreateTestAbsenceType("AQT200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceQuotaDto
            {
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceType.Id,
                EntitlementFrom = new DateTime(2024, 5, 1),
                EntitlementTo = new DateTime(2024, 1, 1),
                DeductionFrom = new DateTime(2024, 5, 1),
                DeductionTo = new DateTime(2024, 1, 1)
            }));
    }
}
