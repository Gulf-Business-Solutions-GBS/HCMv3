using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeAbsAdjustmentsAppService_Tests : AppTestBase
{
    private static readonly DateTime AdjustDate = new(2024, 3, 1);

    private readonly IEmployeeAbsAdjustmentsAppService _appService;

    public EmployeeAbsAdjustmentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAbsAdjustmentsAppService>();
    }

    #region Helpers

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

    private AbsenceType CreateTestAbsenceType(string code, QuotaType quotaType = QuotaType.None,
        bool enableNegativeBalance = false, int negativeBalanceLimit = 0)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = "Type " + code,
                NameAr = "Type " + code,
                QuotaType = quotaType,
                EnableNegativeBalance = enableNegativeBalance,
                NegativeBalanceLimit = negativeBalanceLimit
            }).Entity;
        });
        return absenceType;
    }

    private int CreateQuota(int employeeId, int absenceTypeId, double accrued, double used = 0, double adjusted = 0)
    {
        EmployeeAbsenceQuota quota = null;
        UsingDbContext(context =>
        {
            quota = context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = new DateTime(2024, 1, 1),
                EntitlementTo = new DateTime(2024, 12, 31),
                DeductionFrom = new DateTime(2024, 1, 1),
                DeductionTo = new DateTime(2025, 12, 31),
                QuotaAccrued = accrued,
                QuotaUsed = used,
                QuotaAdjusted = adjusted
            }).Entity;
        });
        return quota.Id;
    }

    private double AdjustedOn(int quotaId) =>
        UsingDbContext(context => context.Set<EmployeeAbsenceQuota>().Single(q => q.Id == quotaId).QuotaAdjusted);

    private void SetEarliestEvaluationDate(DateTime date)
    {
        UsingDbContext(context =>
        {
            context.Set<HCMv3.SystemSettings.ManageTimeSettings>().Add(new HCMv3.SystemSettings.ManageTimeSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EarliestEvaluationDate = date
            });
        });
    }

    private Task AdjustAsync(int employeeId, int absenceTypeId, double days,
        AdjustmentType type = AdjustmentType.Adjust, DateTime? date = null, int? id = null) =>
        _appService.CreateOrEdit(new CreateOrEditEmployeeAbsAdjustmentDto
        {
            Id = id,
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            AdjustmentType = type,
            EffectiveDate = date ?? AdjustDate,
            QuotaAdjusted = days,
            AdjustmentPaymentType = AdjustmentPaymentType.PayWithPayroll
        });

    private async Task<int> OnlyAdjustmentIdAsync(int employeeId) =>
        (await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { EmployeeId = employeeId })).Items.Single().Id;

    #endregion

    #region CRUD

    [Fact]
    public async Task Should_Create_Adjustment()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA100");
        var absenceType = CreateTestAbsenceType("AAT100");

        //Act
        await AdjustAsync(employee.Id, absenceType.Id, 2.5);

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaAdjusted.ShouldBe(2.5);
        result.Items[0].AdjustmentType.ShouldBe(AdjustmentType.Adjust);
        result.Items[0].EmployeeCode.ShouldBe("EAA100");
    }

    [Fact]
    public async Task Should_Update_Adjustment()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA200");
        var absenceType = CreateTestAbsenceType("AAT200");
        await AdjustAsync(employee.Id, absenceType.Id, 1, AdjustmentType.Encashment);
        var id = await OnlyAdjustmentIdAsync(employee.Id);

        //Act
        await AdjustAsync(employee.Id, absenceType.Id, 3, AdjustmentType.Encashment, id: id);

        //Assert
        var updated = await _appService.GetEmployeeAbsAdjustmentForEdit(new EntityDto(id));
        updated.EmployeeAbsAdjustment.QuotaAdjusted.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Return_Employee_Code_And_Name_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA210");
        var absenceType = CreateTestAbsenceType("AAT210");
        await AdjustAsync(employee.Id, absenceType.Id, 1);
        var id = await OnlyAdjustmentIdAsync(employee.Id);

        //Act
        var output = await _appService.GetEmployeeAbsAdjustmentForEdit(new EntityDto(id));

        //Assert
        output.EmployeeCodeWithFullNameEn.ShouldBe("EAA210 F L");
    }

    [Fact]
    public async Task Should_Round_QuotaAdjusted_To_Six_Decimals()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA220");
        var absenceType = CreateTestAbsenceType("AAT220");

        //Act
        await AdjustAsync(employee.Id, absenceType.Id, 1.12345678);

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { EmployeeId = employee.Id });
        result.Items[0].QuotaAdjusted.ShouldBe(1.123457);
    }

    [Fact]
    public async Task Should_Refuse_An_Empty_Effective_Date()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA230");
        var absenceType = CreateTestAbsenceType("AAT230");

        //Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() => _appService.CreateOrEdit(new CreateOrEditEmployeeAbsAdjustmentDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            QuotaAdjusted = 1
        }));
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Code_Or_Absence_Type()
    {
        //Arrange
        var first = CreateTestEmployee("EAA240");
        var second = CreateTestEmployee("EAA250");
        var typeA = CreateTestAbsenceType("AAT240");
        var typeB = CreateTestAbsenceType("AAT250");
        await AdjustAsync(first.Id, typeA.Id, 1);
        await AdjustAsync(second.Id, typeB.Id, 1);

        //Act
        var byCode = await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { Filter = "EAA240", MaxResultCount = 10 });
        var byType = await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { Filter = "AAT250", MaxResultCount = 10 });

        //Assert
        byCode.Items.Single().EmployeeId.ShouldBe(first.Id);
        byType.Items.Single().EmployeeId.ShouldBe(second.Id);
    }

    #endregion

    #region Quota calculation -- legacy EmployeeAbsAdjustment.UpdateQuotaBalance / OnDeleting

    [Fact]
    public async Task Should_Add_Adjustment_To_Annual_Quota_On_Create()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA300");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30, adjusted: 1);

        //Act
        await AdjustAsync(employee.Id, annual.Id, 4);

        //Assert
        AdjustedOn(quotaId).ShouldBe(5);
    }

    [Theory]
    [InlineData(AdjustmentType.Encashment)]
    [InlineData(AdjustmentType.Adjust)]
    [InlineData(AdjustmentType.Fractional)]
    public async Task Should_Move_Quota_For_Every_Type_Except_NoImpactOnQuota(AdjustmentType type)
    {
        //Arrange
        var employee = CreateTestEmployee("EAA310");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30);

        //Act
        await AdjustAsync(employee.Id, annual.Id, 2, type);

        //Assert
        AdjustedOn(quotaId).ShouldBe(2);
    }

    [Fact]
    public async Task Should_Not_Touch_Quota_For_NoImpactOnQuota()
    {
        //Arrange: even more than the balance passes, as the check is skipped too.
        var employee = CreateTestEmployee("EAA320");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 2);

        //Act
        await AdjustAsync(employee.Id, annual.Id, 9, AdjustmentType.NoImpactOnQuota);

        //Assert
        AdjustedOn(quotaId).ShouldBe(0);
        (await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Not_Touch_Quota_For_A_Non_Annual_Type()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA330");
        var sick = CreateTestAbsenceType("SICK330");
        var quotaId = CreateQuota(employee.Id, sick.Id, accrued: 10);

        //Act
        await AdjustAsync(employee.Id, sick.Id, 3);

        //Assert
        AdjustedOn(quotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Charge_The_Code_1_Quota_For_Another_AnnualLeave_Type()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA340");
        var code1 = CreateTestAbsenceType("1");
        var otherAnnual = CreateTestAbsenceType("AL2", QuotaType.AnnualLeave);
        var code1QuotaId = CreateQuota(employee.Id, code1.Id, accrued: 30);
        var otherQuotaId = CreateQuota(employee.Id, otherAnnual.Id, accrued: 30);

        //Act
        await AdjustAsync(employee.Id, otherAnnual.Id, 3);

        //Assert
        AdjustedOn(code1QuotaId).ShouldBe(3);
        AdjustedOn(otherQuotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_Adjustment_Greater_Than_Balance()
    {
        //Arrange: balance = 30 - (28 + 0 + 0) = 2.
        var employee = CreateTestEmployee("EAA350");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30, used: 28);

        //Act
        var ex = await Should.ThrowAsync<UserFriendlyException>(() => AdjustAsync(employee.Id, annual.Id, 3));

        //Assert
        ex.Details.ShouldContain("2");
        ex.Details.ShouldContain("3");
        AdjustedOn(quotaId).ShouldBe(0);
        (await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Allow_Adjustment_Equal_To_Balance()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA360");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10, used: 4);

        //Act
        await AdjustAsync(employee.Id, annual.Id, 6);

        //Assert
        AdjustedOn(quotaId).ShouldBe(6);
    }

    [Fact]
    public async Task Should_Allow_Going_Negative_Up_To_The_Negative_Balance_Limit()
    {
        //Arrange: balance 2 + limit 5 = 7.
        var employee = CreateTestEmployee("EAA370");
        var annual = CreateTestAbsenceType("1", enableNegativeBalance: true, negativeBalanceLimit: 5);
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10, used: 8);

        //Act
        await AdjustAsync(employee.Id, annual.Id, 7);

        //Assert
        AdjustedOn(quotaId).ShouldBe(7);
    }

    [Fact]
    public async Task Should_Refuse_Beyond_The_Negative_Balance_Limit_And_Show_It()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA380");
        var annual = CreateTestAbsenceType("1", enableNegativeBalance: true, negativeBalanceLimit: 5);
        CreateQuota(employee.Id, annual.Id, accrued: 10, used: 8);

        //Act
        var ex = await Should.ThrowAsync<UserFriendlyException>(() => AdjustAsync(employee.Id, annual.Id, 7.5));

        //Assert: Available 2, limit 5, remaining 7, applied 7.5.
        ex.Details.ShouldContain("Negative Balance Limit : 5");
        ex.Details.ShouldContain("Remaining Quota : 7");
        ex.Details.ShouldContain("Applied Quota : 7.5 days");
    }

    [Fact]
    public async Task Should_Accept_A_Negative_Adjustment_And_Give_Days_Back()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA390");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 0);

        //Act
        await AdjustAsync(employee.Id, annual.Id, -2);

        //Assert
        AdjustedOn(quotaId).ShouldBe(-2);
    }

    [Fact]
    public async Task Should_Throw_When_No_Quota_Covers_The_Effective_Date()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA400");
        var annual = CreateTestAbsenceType("1");
        CreateQuota(employee.Id, annual.Id, accrued: 30);

        //Act / Assert
        await Should.ThrowAsync<UserFriendlyException>(() => AdjustAsync(employee.Id, annual.Id, 1, date: new DateTime(2025, 2, 1)));
    }

    [Fact]
    public async Task Should_Not_Touch_Quota_Before_Earliest_Evaluation_Date()
    {
        //Arrange
        SetEarliestEvaluationDate(new DateTime(2024, 6, 1));
        var employee = CreateTestEmployee("EAA410");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30);

        //Act
        await AdjustAsync(employee.Id, annual.Id, 3, date: new DateTime(2024, 5, 31));

        //Assert
        AdjustedOn(quotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Move_Only_The_Difference_On_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA420");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10);
        await AdjustAsync(employee.Id, annual.Id, 2);
        var id = await OnlyAdjustmentIdAsync(employee.Id);

        //Act: balance after giving back 2 is 10, so 9 passes.
        await AdjustAsync(employee.Id, annual.Id, 9, id: id);

        //Assert
        AdjustedOn(quotaId).ShouldBe(9);
    }

    [Fact]
    public async Task Should_Give_Back_When_Edited_To_NoImpactOnQuota()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA430");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10);
        await AdjustAsync(employee.Id, annual.Id, 4);
        var id = await OnlyAdjustmentIdAsync(employee.Id);

        //Act
        await AdjustAsync(employee.Id, annual.Id, 4, AdjustmentType.NoImpactOnQuota, id: id);

        //Assert
        AdjustedOn(quotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Charge_When_Edited_From_NoImpactOnQuota()
    {
        //Arrange: the previous NoImpactOnQuota value was never charged, so nothing is given back.
        var employee = CreateTestEmployee("EAA440");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10);
        await AdjustAsync(employee.Id, annual.Id, 4, AdjustmentType.NoImpactOnQuota);
        var id = await OnlyAdjustmentIdAsync(employee.Id);

        //Act
        await AdjustAsync(employee.Id, annual.Id, 4, AdjustmentType.Adjust, id: id);

        //Assert
        AdjustedOn(quotaId).ShouldBe(4);
    }

    [Fact]
    public async Task Should_Give_Back_Previous_Value_When_Edited_To_Zero()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA450");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10);
        await AdjustAsync(employee.Id, annual.Id, 3);
        var id = await OnlyAdjustmentIdAsync(employee.Id);

        //Act
        await AdjustAsync(employee.Id, annual.Id, 0, id: id);

        //Assert
        AdjustedOn(quotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Give_Back_When_Edited_To_A_Non_Annual_Type()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA460");
        var annual = CreateTestAbsenceType("1");
        var sick = CreateTestAbsenceType("SICK460");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10);
        await AdjustAsync(employee.Id, annual.Id, 3);
        var id = await OnlyAdjustmentIdAsync(employee.Id);

        //Act
        await AdjustAsync(employee.Id, sick.Id, 3, id: id);

        //Assert
        AdjustedOn(quotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Give_Back_On_Delete()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA470");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10, adjusted: 1);
        await AdjustAsync(employee.Id, annual.Id, 3);
        var id = await OnlyAdjustmentIdAsync(employee.Id);

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        AdjustedOn(quotaId).ShouldBe(1);
        (await _appService.GetAll(new GetAllEmployeeAbsAdjustmentsInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Touch_Quota_On_Delete_Of_NoImpactOnQuota()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA480");
        var annual = CreateTestAbsenceType("1");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10, adjusted: 1);
        await AdjustAsync(employee.Id, annual.Id, 3, AdjustmentType.NoImpactOnQuota);
        var id = await OnlyAdjustmentIdAsync(employee.Id);

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        AdjustedOn(quotaId).ShouldBe(1);
    }

    #endregion
}
