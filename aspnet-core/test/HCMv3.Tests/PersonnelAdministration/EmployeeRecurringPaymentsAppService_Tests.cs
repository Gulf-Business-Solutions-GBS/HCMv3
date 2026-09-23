using System;
using System.Linq;
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

    private void SetEffectiveDateInRecPayment(bool enabled)
    {
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var settings = context.Set<HCMv3.SystemSettings.ManagePayrollSettings>().FirstOrDefault(s => s.TenantId == tenantId)
                           ?? context.Set<HCMv3.SystemSettings.ManagePayrollSettings>()
                               .Add(new HCMv3.SystemSettings.ManagePayrollSettings { TenantId = tenantId }).Entity;
            settings.EnableEffectiveDateInRecPayment = enabled;
        });
    }

    /// <summary>
    /// Legacy "Auto EndDate" (Zoho #2783): with the setting on, a new open-ended entry closes the previous
    /// open-ended one the day before it starts instead of being refused as an overlap.
    /// </summary>
    [Fact]
    public async Task With_Effective_Date_On_A_New_Open_Ended_Entry_Should_Close_The_Previous_One()
    {
        var employee = CreateTestEmployee("ERP300");
        var wageType = CreateTestWageType("WT400");
        SetEffectiveDateInRecPayment(true);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 100,
            StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
        });
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 150,
            StartDate = new DateTime(2024, 6, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
        });

        var rows = (await _appService.GetAll(new GetAllEmployeeRecurringPaymentsInput { EmployeeId = employee.Id })).Items
            .OrderBy(r => r.StartDate).ToList();
        rows.Count.ShouldBe(2);
        rows[0].EndDate.ShouldBe(new DateTime(2024, 5, 31));
        rows[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task With_Effective_Date_On_A_New_Entry_Starting_Before_The_Open_One_Is_Refused()
    {
        var employee = CreateTestEmployee("ERP310");
        var wageType = CreateTestWageType("WT410");
        SetEffectiveDateInRecPayment(true);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 100,
            StartDate = new DateTime(2024, 6, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
        });

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(
            new CreateOrEditEmployeeRecurringPaymentDto
            {
                EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 150,
                StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
            }));
        exception.Message.ShouldContain("Start Date");
    }

    [Fact]
    public async Task With_Effective_Date_Off_An_Overlapping_Open_Ended_Entry_Is_Still_Refused()
    {
        var employee = CreateTestEmployee("ERP320");
        var wageType = CreateTestWageType("WT420");
        SetEffectiveDateInRecPayment(false);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRecurringPaymentDto
        {
            EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 100,
            StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(
            new CreateOrEditEmployeeRecurringPaymentDto
            {
                EmployeeId = employee.Id, WageTypeId = wageType.Id, Amount = 150,
                StartDate = new DateTime(2024, 6, 1), EndDate = new DateTime(9999, 12, 31), PayInterval = PayInterval.Monthly
            }));
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
