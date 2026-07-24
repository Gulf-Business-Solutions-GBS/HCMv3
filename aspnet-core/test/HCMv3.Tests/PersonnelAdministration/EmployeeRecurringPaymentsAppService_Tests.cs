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
public class EmployeeRecurringPaymentsAppService_Tests : AppTestBase
{
    private readonly IEmployeeRecurringPaymentsAppService _appService;

    public EmployeeRecurringPaymentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeRecurringPaymentsAppService>();
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

    private WageType CreateTestWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                PayrollOrder = 1
            }).Entity;
        });
        return wageType;
    }

    [Fact]
    public async Task Should_Create_EmployeeRecurringPayment()
    {
        //Arrange
        var employee = CreateTestEmployee("ERP100");
        var wageType = CreateTestWageType("WT200");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id,
            WageTypeId = wageType.Id,
            Amount = 100,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            PayInterval = PayInterval.Monthly
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeRecurringPaymentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Amount_Not_GreaterThan_Zero()
    {
        //Arrange
        var employee = CreateTestEmployee("ERP200");
        var wageType = CreateTestWageType("WT300");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
            {
                EmployeeId = employee.Id,
                WageTypeId = wageType.Id,
                Amount = 0,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(9999, 12, 31)
            }));
    }
}
