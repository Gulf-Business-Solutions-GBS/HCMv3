using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Indemnity screen. Saving/deleting moves <c>EmployeeGratuityQuota.QuotaUsed</c> exactly as legacy
/// <c>EmployeeGratuity.UpdateQuotaBalance</c>/<c>OnDeleting</c>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeGratuitiesAppService_Tests : AppTestBase
{
    private readonly IEmployeeGratuitiesAppService _appService;

    public EmployeeGratuitiesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeGratuitiesAppService>();
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

    private EmployeeGratuityQuota CreateQuota(int employeeId, double accrued, double used = 0, double adjusted = 0,
        DateTime? deductionFrom = null, DateTime? deductionTo = null)
    {
        EmployeeGratuityQuota quota = null;
        UsingDbContext(context =>
        {
            quota = context.EmployeeGratuityQuotas.Add(new EmployeeGratuityQuota
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                EntitlementFrom = new DateTime(2020, 1, 1),
                EntitlementTo = new DateTime(9999, 12, 31),
                DeductionFrom = deductionFrom ?? new DateTime(2020, 10, 1),
                DeductionTo = deductionTo ?? new DateTime(9999, 12, 31),
                QuotaAccrued = accrued,
                QuotaUsed = used,
                QuotaAdjusted = adjusted
            }).Entity;
        });
        return quota;
    }

    private double QuotaUsedOf(int quotaId)
    {
        double used = 0;
        UsingDbContext(context => used = context.EmployeeGratuityQuotas.Single(q => q.Id == quotaId).QuotaUsed);
        return used;
    }

    private async Task<int> CreateGratuityAsync(int employeeId, DateTime paidDate, double days)
    {
        await _appService.CreateOrEdit(new CreateOrEditEmployeeGratuityDto
        {
            EmployeeId = employeeId,
            GratuityPaidDate = paidDate,
            GratuityPaidDays = days
        });
        var all = await _appService.GetAll(new GetAllEmployeeGratuitiesInput { EmployeeId = employeeId });
        return all.Items.OrderByDescending(i => i.Id).First().Id;
    }

    [Fact]
    public async Task Should_Create_Gratuity_And_Charge_Quota_Used()
    {
        //Arrange
        var employee = CreateTestEmployee("GR100");
        var quota = CreateQuota(employee.Id, accrued: 100);

        //Act
        await CreateGratuityAsync(employee.Id, new DateTime(2024, 6, 1), 10);

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeGratuitiesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].GratuityPaidDays.ShouldBe(10);
        QuotaUsedOf(quota.Id).ShouldBe(10);
    }

    [Fact]
    public async Task Should_Return_Comments_And_Reset_Flag_In_List()
    {
        //Arrange
        var employee = CreateTestEmployee("GR110");
        CreateQuota(employee.Id, accrued: 100);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeGratuityDto
        {
            EmployeeId = employee.Id,
            GratuityPaidDate = new DateTime(2024, 6, 1),
            GratuityPaidDays = 1,
            ResetGratuityCalc = true,
            Comments = "Paid"
        });

        //Assert
        var item = (await _appService.GetAll(new GetAllEmployeeGratuitiesInput { EmployeeId = employee.Id })).Items[0];
        item.Comments.ShouldBe("Paid");
        item.ResetGratuityCalc.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_When_Days_Exceed_Quota_Balance()
    {
        //Arrange -- balance = 30 - (10 + 5) = 15
        var employee = CreateTestEmployee("GR120");
        var quota = CreateQuota(employee.Id, accrued: 30, used: 10, adjusted: 5);

        //Act
        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => CreateGratuityAsync(employee.Id, new DateTime(2024, 6, 1), 15.5));

        //Assert
        ex.Message.ShouldContain("15");
        QuotaUsedOf(quota.Id).ShouldBe(10);
    }

    [Fact]
    public async Task Should_Allow_Days_Equal_To_Quota_Balance()
    {
        //Arrange
        var employee = CreateTestEmployee("GR130");
        var quota = CreateQuota(employee.Id, accrued: 30, used: 10, adjusted: 5);

        //Act
        await CreateGratuityAsync(employee.Id, new DateTime(2024, 6, 1), 15);

        //Assert
        QuotaUsedOf(quota.Id).ShouldBe(25);
    }

    [Fact]
    public async Task Should_Refuse_When_No_Quota_Covers_Paid_Date()
    {
        //Arrange -- the deduction period starts after the paid date
        var employee = CreateTestEmployee("GR140");
        CreateQuota(employee.Id, accrued: 100, deductionFrom: new DateTime(2025, 1, 1));

        //Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(
            () => CreateGratuityAsync(employee.Id, new DateTime(2024, 6, 1), 5));
    }

    [Fact]
    public async Task Should_Not_Need_Quota_For_Zero_Days()
    {
        //Arrange
        var employee = CreateTestEmployee("GR150");

        //Act
        await CreateGratuityAsync(employee.Id, new DateTime(2024, 6, 1), 0);

        //Assert
        (await _appService.GetAll(new GetAllEmployeeGratuitiesInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Give_Back_Old_Days_And_Charge_New_Days_On_Edit()
    {
        //Arrange -- balance 20; after the first save used = 15, balance 5
        var employee = CreateTestEmployee("GR160");
        var quota = CreateQuota(employee.Id, accrued: 20);
        var id = await CreateGratuityAsync(employee.Id, new DateTime(2024, 6, 1), 15);

        //Act -- 18 only fits because the 15 is given back first
        await _appService.CreateOrEdit(new CreateOrEditEmployeeGratuityDto
        {
            Id = id,
            EmployeeId = employee.Id,
            GratuityPaidDate = new DateTime(2024, 6, 1),
            GratuityPaidDays = 18
        });

        //Assert
        QuotaUsedOf(quota.Id).ShouldBe(18);
    }

    [Fact]
    public async Task Should_Move_Days_Between_Quotas_When_Paid_Date_Changes()
    {
        //Arrange
        var employee = CreateTestEmployee("GR170");
        var first = CreateQuota(employee.Id, accrued: 50,
            deductionFrom: new DateTime(2020, 10, 1), deductionTo: new DateTime(2023, 12, 31));
        var second = CreateQuota(employee.Id, accrued: 50,
            deductionFrom: new DateTime(2024, 1, 1), deductionTo: new DateTime(9999, 12, 31));
        var id = await CreateGratuityAsync(employee.Id, new DateTime(2023, 6, 1), 10);

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeGratuityDto
        {
            Id = id,
            EmployeeId = employee.Id,
            GratuityPaidDate = new DateTime(2024, 6, 1),
            GratuityPaidDays = 10
        });

        //Assert
        QuotaUsedOf(first.Id).ShouldBe(0);
        QuotaUsedOf(second.Id).ShouldBe(10);
    }

    [Fact]
    public async Task Should_Delete_Gratuity_And_Give_Days_Back()
    {
        //Arrange
        var employee = CreateTestEmployee("GR200");
        var quota = CreateQuota(employee.Id, accrued: 100);
        var id = await CreateGratuityAsync(employee.Id, new DateTime(2024, 6, 1), 5);

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeGratuitiesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
        QuotaUsedOf(quota.Id).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Get_Gratuity_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("GR210");
        CreateQuota(employee.Id, accrued: 100);
        var id = await CreateGratuityAsync(employee.Id, new DateTime(2024, 6, 1), 7);

        //Act
        var output = await _appService.GetEmployeeGratuityForEdit(new EntityDto(id));

        //Assert
        output.EmployeeGratuity.Id.ShouldBe(id);
        output.EmployeeGratuity.GratuityPaidDays.ShouldBe(7);
        output.EmployeeGratuity.GratuityPaidDate.ShouldBe(new DateTime(2024, 6, 1));
    }
}
