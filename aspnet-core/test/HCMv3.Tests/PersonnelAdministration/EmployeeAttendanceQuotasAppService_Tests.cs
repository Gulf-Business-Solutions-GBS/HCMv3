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
public class EmployeeAttendanceQuotasAppService_Tests : AppTestBase
{
    private readonly IEmployeeAttendanceQuotasAppService _appService;

    public EmployeeAttendanceQuotasAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAttendanceQuotasAppService>();
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
    public async Task Should_Create_Quota_And_Compute_Balance()
    {
        //Arrange
        var employee = CreateTestEmployee("EATQ100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceQuotaDto
        {
            EmployeeId = employee.Id,
            EntitlementFrom = new DateTime(2024, 1, 1),
            EntitlementTo = new DateTime(9999, 12, 31),
            DeductionFrom = new DateTime(2024, 1, 1),
            DeductionTo = new DateTime(9999, 12, 31),
            QuotaAccrued = 10,
            QuotaUsed = 3
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAttendanceQuotasInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaBalance.ShouldBe(7);
    }

    [Fact]
    public async Task Should_Throw_When_EntitlementTo_Earlier_Than_EntitlementFrom()
    {
        //Arrange
        var employee = CreateTestEmployee("EATQ200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceQuotaDto
            {
                EmployeeId = employee.Id,
                EntitlementFrom = new DateTime(2024, 6, 1),
                EntitlementTo = new DateTime(2024, 1, 1),
                DeductionFrom = new DateTime(2024, 6, 1),
                DeductionTo = new DateTime(2024, 1, 1)
            }));
    }
}
