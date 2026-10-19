using System;
using System.Linq;
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

    // ------------------------------------------------------------------------------------------------------
    // Quota cascade -- legacy EmployeeAbsQuotaForfeit.cs OnSaving (117-152), UpdateQuotaBalance (162-231),
    // OnDeleting (232-277). Expected values are worked from those lines.
    // ------------------------------------------------------------------------------------------------------

    private static readonly DateTime ForfeitDate = new(2024, 12, 31);

    private AbsenceType CreateAnnualType(string code = "1", QuotaType quotaType = QuotaType.None)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = "Annual " + code,
                NameAr = "Annual " + code,
                QuotaType = quotaType
            }).Entity;
        });
        return absenceType;
    }

    private int CreateQuota(int employeeId, int absenceTypeId, double accrued, double used = 0, double forfeited = 0)
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
                QuotaForfeited = forfeited
            }).Entity;
        });
        return quota.Id;
    }

    private double ForfeitedOn(int quotaId) =>
        UsingDbContext(context => context.Set<EmployeeAbsenceQuota>().Single(q => q.Id == quotaId).QuotaForfeited);

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

    private Task ForfeitAsync(int employeeId, int absenceTypeId, double days, DateTime? date = null,
        int? id = null, bool dontUpdate = false) =>
        _appService.CreateOrEdit(new CreateOrEditEmployeeAbsQuotaForfeitDto
        {
            Id = id,
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            EffectiveDate = date ?? ForfeitDate,
            QuotaForfeited = days,
            DontUpdateQuotaBalance = dontUpdate
        });

    private async Task<int> OnlyForfeitIdAsync(int employeeId) =>
        (await _appService.GetAll(new GetAllEmployeeAbsQuotaForfeitsInput { EmployeeId = employeeId })).Items.Single().Id;

    [Fact]
    public async Task Should_Add_Forfeit_To_Annual_Quota_On_Create()
    {
        // Legacy line 223: QuotaForfeited = QuotaForfeited + this.QuotaForfeited.
        var employee = CreateTestEmployee("EQF300");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30, forfeited: 1);

        await ForfeitAsync(employee.Id, annual.Id, 4);

        ForfeitedOn(quotaId).ShouldBe(5);
    }

    [Fact]
    public async Task Should_Not_Touch_Quota_When_DontUpdateQuotaBalance_Is_Ticked()
    {
        // Legacy line 141: the whole UpdateQuotaBalance is skipped -- including the negative-balance check.
        var employee = CreateTestEmployee("EQF310");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 2);

        await ForfeitAsync(employee.Id, annual.Id, 9, dontUpdate: true);

        ForfeitedOn(quotaId).ShouldBe(0);
        (await _appService.GetAll(new GetAllEmployeeAbsQuotaForfeitsInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_Forfeit_Greater_Than_Balance()
    {
        // Legacy line 219-220: balance = 30 - (28 + 0 + 0) = 2; forfeiting 3 > 2 is refused.
        var employee = CreateTestEmployee("EQF320");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30, used: 28);

        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(() => ForfeitAsync(employee.Id, annual.Id, 3));

        ForfeitedOn(quotaId).ShouldBe(0);
        (await _appService.GetAll(new GetAllEmployeeAbsQuotaForfeitsInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Allow_Forfeit_Equal_To_Balance()
    {
        // Legacy line 219 is a strict ">", so forfeiting exactly the balance passes.
        var employee = CreateTestEmployee("EQF330");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10, used: 4);

        await ForfeitAsync(employee.Id, annual.Id, 6);

        ForfeitedOn(quotaId).ShouldBe(6);
    }

    [Fact]
    public async Task Should_Accept_Negative_Forfeit_And_Reduce_QuotaForfeited()
    {
        // LEGACY ODDITY (line 219): only "> balance" is refused, so a negative forfeit passes and gives days
        // back; line 223 then adds the negative value. Kept as-is under the parity rule.
        var employee = CreateTestEmployee("EQF340");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 0);

        await ForfeitAsync(employee.Id, annual.Id, -2);

        ForfeitedOn(quotaId).ShouldBe(-2);
    }

    [Fact]
    public async Task Should_Move_Difference_On_Edit()
    {
        // Lines 189 then 223: 4 is given back, 6 is charged -> 6.
        var employee = CreateTestEmployee("EQF350");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30);
        await ForfeitAsync(employee.Id, annual.Id, 4);

        await ForfeitAsync(employee.Id, annual.Id, 6, id: await OnlyForfeitIdAsync(employee.Id));

        ForfeitedOn(quotaId).ShouldBe(6);
    }

    [Fact]
    public async Task Should_Check_Balance_After_Giving_Back_Previous_Value_On_Edit()
    {
        // Lines 215-219: same quota row -> the check uses the already-credited row. Accrued 10, forfeit 8 leaves
        // balance 2; editing to 10 credits 8 back first (balance 10), and 10 > 10 is false -> allowed.
        var employee = CreateTestEmployee("EQF360");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 10);
        await ForfeitAsync(employee.Id, annual.Id, 8);

        await ForfeitAsync(employee.Id, annual.Id, 10, id: await OnlyForfeitIdAsync(employee.Id));

        ForfeitedOn(quotaId).ShouldBe(10);
    }

    [Fact]
    public async Task Should_Not_Move_Quota_On_Edit_When_DontUpdateQuotaBalance_Is_Ticked()
    {
        var employee = CreateTestEmployee("EQF370");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30);
        await ForfeitAsync(employee.Id, annual.Id, 4);

        await ForfeitAsync(employee.Id, annual.Id, 6, id: await OnlyForfeitIdAsync(employee.Id), dontUpdate: true);

        ForfeitedOn(quotaId).ShouldBe(4);
    }

    [Fact]
    public async Task Should_Release_Previous_Value_When_Edited_To_Zero()
    {
        // Line 189 runs, line 198 (QuotaForfeited != 0) skips the charge.
        var employee = CreateTestEmployee("EQF380");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30);
        await ForfeitAsync(employee.Id, annual.Id, 4);

        await ForfeitAsync(employee.Id, annual.Id, 0, id: await OnlyForfeitIdAsync(employee.Id));

        ForfeitedOn(quotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Release_Previous_Value_When_Edited_To_Non_Annual_Type()
    {
        // Lines 175-191 credit the annual quota; lines 200-228 then skip (not annual).
        var employee = CreateTestEmployee("EQF390");
        var annual = CreateAnnualType();
        var other = CreateTestAbsenceType("QFT390");
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30);
        var otherQuotaId = CreateQuota(employee.Id, other.Id, accrued: 30);
        await ForfeitAsync(employee.Id, annual.Id, 4);

        await ForfeitAsync(employee.Id, other.Id, 4, id: await OnlyForfeitIdAsync(employee.Id));

        ForfeitedOn(quotaId).ShouldBe(0);
        ForfeitedOn(otherQuotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Give_Forfeit_Back_On_Delete()
    {
        // Legacy OnDeleting line 262.
        var employee = CreateTestEmployee("EQF400");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30);
        await ForfeitAsync(employee.Id, annual.Id, 4);

        await _appService.Delete(new Abp.Application.Services.Dto.EntityDto(await OnlyForfeitIdAsync(employee.Id)));

        ForfeitedOn(quotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Give_Back_On_Delete_Even_When_Saved_Without_Updating_Quota()
    {
        // LEGACY ODDITY: DontUpdateQuotaBalance is [NonPersistent] (line 61), so a row loaded for deletion always
        // has it false (line 236) and OnDeleting subtracts the stored forfeit without a floor (line 262) -- even
        // though that forfeit was never charged. Quota goes to -4. Kept as-is under the parity rule.
        var employee = CreateTestEmployee("EQF410");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30);
        await ForfeitAsync(employee.Id, annual.Id, 4, dontUpdate: true);

        await _appService.Delete(new Abp.Application.Services.Dto.EntityDto(await OnlyForfeitIdAsync(employee.Id)));

        ForfeitedOn(quotaId).ShouldBe(-4);
    }

    [Fact]
    public async Task Should_Charge_Code_1_Quota_For_Other_AnnualLeave_Group_Type()
    {
        // LEGACY ODDITY (lines 200-210): a type in the AnnualLeave quota group charges the quota of the Code "1"
        // type, not its own. Kept as-is under the parity rule.
        var employee = CreateTestEmployee("EQF420");
        var code1 = CreateAnnualType();
        var grouped = CreateAnnualType(code: "AL2", quotaType: QuotaType.AnnualLeave);
        var code1QuotaId = CreateQuota(employee.Id, code1.Id, accrued: 30);
        var ownQuotaId = CreateQuota(employee.Id, grouped.Id, accrued: 30);

        await ForfeitAsync(employee.Id, grouped.Id, 3);

        ForfeitedOn(code1QuotaId).ShouldBe(3);
        ForfeitedOn(ownQuotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_Annual_Forfeit_Without_Quota_Row()
    {
        // Legacy lines 211-212.
        var employee = CreateTestEmployee("EQF430");
        var annual = CreateAnnualType();

        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(() => ForfeitAsync(employee.Id, annual.Id, 1));
    }

    [Fact]
    public async Task Should_Not_Require_Quota_For_Non_Annual_Type()
    {
        // Legacy line 200: non-annual forfeits never touch a quota, even one of their own type.
        var employee = CreateTestEmployee("EQF440");
        var other = CreateTestAbsenceType("QFT440");
        var otherQuotaId = CreateQuota(employee.Id, other.Id, accrued: 1);

        await ForfeitAsync(employee.Id, other.Id, 5);

        ForfeitedOn(otherQuotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Charge_Quota_Before_Earliest_Evaluation_Date()
    {
        // Legacy line 203: EffectiveDate < EarliestEvaluationDate -> no charge (and no quota row needed).
        SetEarliestEvaluationDate(new DateTime(2025, 1, 1));
        var employee = CreateTestEmployee("EQF450");
        var annual = CreateAnnualType();
        var quotaId = CreateQuota(employee.Id, annual.Id, accrued: 30);

        await ForfeitAsync(employee.Id, annual.Id, 4);

        ForfeitedOn(quotaId).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Use_Quota_Whose_Entitlement_Covers_Effective_Date()
    {
        // Legacy lines 207-210: EntitlmentFrom <= EffectiveDate <= EntitlmentTo.
        var employee = CreateTestEmployee("EQF460");
        var annual = CreateAnnualType();
        var quota2024 = CreateQuota(employee.Id, annual.Id, accrued: 30);
        EmployeeAbsenceQuota quota2025 = null;
        UsingDbContext(context =>
        {
            quota2025 = context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employee.Id,
                AbsenceTypeId = annual.Id,
                EntitlementFrom = new DateTime(2025, 1, 1),
                EntitlementTo = new DateTime(2025, 12, 31),
                QuotaAccrued = 30
            }).Entity;
        });

        await ForfeitAsync(employee.Id, annual.Id, 2, date: new DateTime(2025, 3, 1));

        ForfeitedOn(quota2024).ShouldBe(0);
        ForfeitedOn(quota2025.Id).ShouldBe(2);
    }
}
