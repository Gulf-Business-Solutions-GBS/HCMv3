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
public class EmployeeGratuityQuotasAppService_Tests : AppTestBase
{
    private readonly IEmployeeGratuityQuotasAppService _appService;

    public EmployeeGratuityQuotasAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeGratuityQuotasAppService>();
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
    public async Task Should_Create_GratuityQuota_With_Computed_Balance()
    {
        //Arrange
        var employee = CreateTestEmployee("GQ100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeGratuityQuotaDto
        {
            EmployeeId = employee.Id,
            EntitlementFrom = new DateTime(2024, 1, 1),
            EntitlementTo = new DateTime(2024, 12, 31),
            DeductionFrom = new DateTime(2024, 1, 1),
            DeductionTo = new DateTime(2024, 12, 31),
            QuotaAccrued = 30,
            QuotaUsed = 5,
            QuotaAdjusted = 2
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeGratuityQuotasInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaBalance.ShouldBe(23);
    }

    [Fact]
    public async Task Should_Throw_When_EntitlementTo_Earlier_Than_EntitlementFrom()
    {
        //Arrange
        var employee = CreateTestEmployee("GQ200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeGratuityQuotaDto
            {
                EmployeeId = employee.Id,
                EntitlementFrom = new DateTime(2024, 12, 31),
                EntitlementTo = new DateTime(2024, 1, 1),
                DeductionFrom = new DateTime(2024, 1, 1),
                DeductionTo = new DateTime(2024, 12, 31)
            }));
    }
}
