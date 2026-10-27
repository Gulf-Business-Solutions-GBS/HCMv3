using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Tests for <see cref="IFullAndFinalSettlementService"/> -- legacy <c>EmployeeFnFPayment</c>'s indemnity
/// suggestion and save side effects.
///
/// <para>The headline behaviour: <b>the provision is the same for resignation and termination; the payment is
/// not.</b> Accrued days come from the gratuity slabs (15 days a year to year 5, then 26); the suggested days to
/// pay are those times the Kuwait rule's percentage for the separation type and service band. Figures are
/// legacy's arithmetic, rounded to 3 decimals -- except that 29 February is not counted as service.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class FullAndFinalSettlementService_Tests : FullAndFinalTestBase
{
    private static readonly DateTime LastDay = new DateTime(2024, 6, 30);

    private readonly IFullAndFinalSettlementService _service;

    public FullAndFinalSettlementService_Tests()
    {
        _service = Resolve<IFullAndFinalSettlementService>();
    }

    #region << Suggestion: the four bands x resignation / termination >>

    /// <summary>
    /// Joining dates 2, 4, 7 and 12 years before 30/06/2024. 29 February is not service, so every full year is
    /// 365 days and earns its rate exactly: 730 x 15/365; 1460 x 15/365;
    /// 75 + 730 x 26/365; 75 + 2555 x 26/365.
    /// </summary>
    [Theory]
    [InlineData("2022-07-01", 30, 0)]            // under 3 years: resignation pays nothing
    [InlineData("2020-07-01", 60, 30)]           // 3-5 years: half
    [InlineData("2017-07-01", 127, 84.671)]       // 5-10 years: two thirds (66.67 %)
    [InlineData("2012-07-01", 257, 257)]         // over 10 years: in full
    public async Task Should_Suggest_Full_Days_On_Termination_And_Reduced_Days_On_Resignation(
        string joining, double terminationDays, double resignationDays)
    {
        var terminated = SeedLeaver(DateTime.Parse(joining), LastDay, SeparationType.Termination);
        var resigned = SeedLeaver(DateTime.Parse(joining), LastDay, SeparationType.Resignation);

        var terminatedBalances = await _service.GetBalancesAsync(terminated.EmployeeId);
        var resignedBalances = await _service.GetBalancesAsync(resigned.EmployeeId);

        terminatedBalances.SuggestedIndemnityDays.ShouldBe(terminationDays);
        resignedBalances.SuggestedIndemnityDays.ShouldBe(resignationDays);
    }

    [Fact]
    public async Task Should_Accrue_The_Same_Provision_For_Resignation_And_Termination()
    {
        var terminated = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        var resigned = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Resignation);

        var terminatedBalances = await _service.GetBalancesAsync(terminated.EmployeeId);
        var resignedBalances = await _service.GetBalancesAsync(resigned.EmployeeId);

        terminatedBalances.GratuityAccrualDays.ShouldBe(60);
        resignedBalances.GratuityAccrualDays.ShouldBe(60);
    }

    /// <summary>
    /// Exactly 3.00 years (1095 days once 29/02/2024 is left out) is the first day of the 50 % band: legacy's bands run 0-2.99 then 3-4.99,
    /// on a year count rounded to 2 decimals. 45 x 50 %.
    /// </summary>
    [Fact]
    public async Task Should_Pay_Half_On_Resignation_From_Exactly_Three_Years()
    {
        var resigned = SeedLeaver(new DateTime(2021, 7, 1), LastDay, SeparationType.Resignation);

        var balances = await _service.GetBalancesAsync(resigned.EmployeeId);

        balances.SuggestedIndemnityDays.ShouldBe(22.5);
    }

    [Fact]
    public async Task Should_Suggest_Zero_When_No_Indemnity_Pay_Rule_Matches()
    {
        var terminated = SeedLeaver(new DateTime(2012, 7, 1), LastDay, SeparationType.Termination, seedKuwaitRules: false);

        var balances = await _service.GetBalancesAsync(terminated.EmployeeId);

        balances.SuggestedIndemnityDays.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Suggest_Zero_When_The_Separation_Was_Revoked()
    {
        var terminated = SeedLeaver(new DateTime(2012, 7, 1), LastDay, SeparationType.Termination);
        UsingDbContext(context => context.Set<EmployeeSeparation>().Find(terminated.SeparationId).RevokeDate = LastDay);

        var balances = await _service.GetBalancesAsync(terminated.EmployeeId);

        balances.SuggestedIndemnityDays.ShouldBe(0);
    }

    /// <summary>18 months capped down to 6: 260 x 6 = 1560 KD = 1560 x 26 / 260 = 156 days.</summary>
    [Fact]
    public async Task Should_Cap_The_Suggestion_At_The_Employee_Group_Maximum()
    {
        var terminated = SeedLeaver(new DateTime(2012, 7, 1), LastDay, SeparationType.Termination, indemnityMaxMonths: 6);

        var balances = await _service.GetBalancesAsync(terminated.EmployeeId);

        balances.SuggestedIndemnityDays.ShouldBe(156);
    }

    /// <summary>
    /// The trap in the Kuwait defaults: a group whose maximum percentage is left at 0 caps indemnity at ZERO
    /// days, not "no cap". Legacy's arithmetic; the percentage must be 100.
    /// </summary>
    [Fact]
    public async Task Should_Suggest_Zero_When_The_Group_Maximum_Percentage_Is_Left_At_Zero()
    {
        var terminated = SeedLeaver(new DateTime(2012, 7, 1), LastDay, SeparationType.Termination, indemnityMaxAccruePercentage: 0);

        var balances = await _service.GetBalancesAsync(terminated.EmployeeId);

        balances.SuggestedIndemnityDays.ShouldBe(0);
    }

    /// <summary>A 3-day unpaid opening balance (transfer type "5") shortens service: 1457 x 15/365.</summary>
    [Fact]
    public async Task Should_Deduct_Unpaid_Days_From_Service()
    {
        var terminated = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        UsingDbContext(context =>
        {
            var transferType = context.Set<TimeTransferType>().Add(new TimeTransferType { TenantId = TenantId, Code = "5", NameEn = "Opening", NameAr = "افتتاحي" }).Entity;
            context.SaveChanges();
            context.Set<TimeTransfer>().Add(new TimeTransfer
            {
                TenantId = TenantId, EmployeeId = terminated.EmployeeId, TransferTypeId = transferType.Id,
                TransferDate = new DateTime(2021, 1, 10), TransferCount = 3
            });
        });

        var balances = await _service.GetBalancesAsync(terminated.EmployeeId);

        balances.SuggestedIndemnityDays.ShouldBe(Math.Round(15.0 / 365 * 1457, 3));
    }

    #endregion

    #region << Slab walk >>

    [Fact]
    public void Should_Count_Fifteen_Days_A_Year_To_Year_Five_Then_Twenty_Six()
    {
        var seeded = SeedLeaver(new DateTime(2012, 7, 1), LastDay, SeparationType.Termination);
        var subGroupId = UsingDbContext(context => context.Set<EmployeeOrgAssignment>().First(a => a.EmployeeId == seeded.EmployeeId).EmployeeSubGroupId);

        var service = Resolve<FullAndFinalSettlementService>();
        using var uow = Resolve<IUnitOfWorkManager>().Begin();

        service.GetIndemnityDaysFromTotalDays(365, LastDay, subGroupId).ShouldBe(15, 0.000001);
        service.GetIndemnityDaysFromTotalDays(1825, LastDay, subGroupId).ShouldBe(75, 0.000001);
        service.GetIndemnityDaysFromTotalDays(1825 + 365, LastDay, subGroupId).ShouldBe(101, 0.000001);
    }

    [Fact]
    public void Should_Return_Zero_Days_Without_A_Sub_Group()
    {
        var service = Resolve<FullAndFinalSettlementService>();
        using var uow = Resolve<IUnitOfWorkManager>().Begin();

        service.GetIndemnityDaysFromTotalDays(1000, LastDay, null).ShouldBe(0);
    }

    #endregion

    #region << Auto-fill >>

    [Fact]
    public async Task Should_Auto_Fill_The_Smaller_Of_Suggested_And_Remaining_When_Enabled()
    {
        SetAutoFill(true);
        var terminated = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedGratuityBalance(terminated.EmployeeId, 50);
        SeedLeaveBalance(terminated.EmployeeId, 30, used: 8);

        var balances = await _service.GetBalancesAsync(terminated.EmployeeId);

        balances.AutoFillGratuityCompensateDays.ShouldBe(50);  // suggested 60, remaining 50
        balances.AutoFillLeaveCompensateDays.ShouldBe(22);
    }

    [Fact]
    public async Task Should_Not_Auto_Fill_When_Disabled()
    {
        SetAutoFill(false);
        var terminated = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedGratuityBalance(terminated.EmployeeId, 50);

        var balances = await _service.GetBalancesAsync(terminated.EmployeeId);

        balances.AutoFillGratuityCompensateDays.ShouldBe(0);
        balances.AutoFillLeaveCompensateDays.ShouldBe(0);
    }

    #endregion

    #region << Validation >>

    [Fact]
    public async Task Should_Reject_An_Entry_Without_An_Exit_Record()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        UsingDbContext(context => context.Set<EmployeeSeparation>().Find(seeded.SeparationId).RevokeDate = LastDay);

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateSaveAsync(Values(seeded.EmployeeId, gratuity: 0), null, null));

        ex.Message.ShouldStartWith("Exit entry not found");
    }

    [Fact]
    public async Task Should_Reject_Indemnity_Days_Above_The_Remaining_Balance()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedGratuityBalance(seeded.EmployeeId, 50);

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateSaveAsync(Values(seeded.EmployeeId, gratuity: 50.5), null, null));

        ex.Message.ShouldBe("You cannot compensate Indemnity days more than available balance.");
    }

    /// <summary>
    /// Re-saving an entry must not fail against its OWN consumption: what the previous save compensated is
    /// added back before comparing.
    /// </summary>
    [Fact]
    public async Task Should_Add_Back_The_Previous_Save_When_Validating_An_Edit()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedGratuityBalance(seeded.EmployeeId, 50);
        UsingDbContext(context => context.Set<EmployeeGratuityQuota>().First(q => q.EmployeeId == seeded.EmployeeId).QuotaUsed = 40);

        var values = Values(seeded.EmployeeId, gratuity: 40);

        await Should.NotThrowAsync(() => _service.ValidateSaveAsync(values, Values(seeded.EmployeeId, gratuity: 40), seeded.SeparationId + 1000));
    }

    [Fact]
    public async Task Should_Reject_A_Second_Full_And_Final_For_The_Same_Exit()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        UsingDbContext(context => context.Set<EmployeeFnFPayment>().Add(new EmployeeFnFPayment
        {
            TenantId = TenantId, EmployeeId = seeded.EmployeeId, CompensationDate = LastDay
        }));

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateSaveAsync(Values(seeded.EmployeeId, gratuity: 0), null, null));

        ex.Message.ShouldStartWith("The Full & Final payment has already been processed");
    }

    /// <summary>
    /// LEGACY QUIRK, pinned: legacy excludes the exit whose id equals the F&amp;F's OWN id (line 756,
    /// <c>x.Oid != this.Oid</c> -- two different tables). So editing an F&amp;F whose id happens to match its
    /// exit's id reports "Exit entry not found". Kept for parity; do not "fix" without a decision.
    /// </summary>
    [Fact]
    public async Task Should_Reject_An_Edit_Whose_Id_Matches_The_Exit_Id_As_Legacy_Does()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateSaveAsync(Values(seeded.EmployeeId, gratuity: 0), null, seeded.SeparationId));

        ex.Message.ShouldStartWith("Exit entry not found");
    }

    /// <summary>
    /// End-of-service payroll already run, previous month still open: the edit lock fires first (legacy
    /// <c>OnDeleting</c> calls <c>ValidatePreviousEntry</c> before its own check).
    /// </summary>
    [Fact]
    public async Task Should_Block_Delete_While_The_Previous_Month_Is_Open_After_The_End_Of_Service_Payroll()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedEndOfServiceRegister(seeded.EmployeeId);

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateDeleteAsync(Values(seeded.EmployeeId, gratuity: 10), 999));

        ex.Message.ShouldStartWith("The salary register has already been processed for this employee");
    }

    /// <summary>With the previous month closed the edit lock passes -- and the hard delete lock still refuses.</summary>
    [Fact]
    public async Task Should_Block_Delete_Once_The_End_Of_Service_Payroll_Has_Run_Even_With_The_Month_Closed()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedEndOfServiceRegister(seeded.EmployeeId);
        UsingDbContext(context => context.Set<PayrollControlRecord>().Add(new PayrollControlRecord
        {
            TenantId = TenantId, PayrollAreaId = seeded.PayrollAreaId, PayrollMonth = 5, PayrollYear = 2024,
            CalendarMonthStart = new DateTime(2024, 5, 1), CalendarMonthEnd = new DateTime(2024, 5, 31),
            PeriodLocked = true, PeriodClosed = true
        }));

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateDeleteAsync(Values(seeded.EmployeeId, gratuity: 10), 999));

        ex.Message.ShouldStartWith("The End of Service payment has already been processed");
    }

    private void SeedEndOfServiceRegister(int employeeId)
    {
        UsingDbContext(context =>
        {
            var reason = context.Set<PayrollReasonType>().Add(new PayrollReasonType { TenantId = TenantId, NameEn = "End Of Service", NameAr = "نهاية الخدمة" }).Entity;
            context.SaveChanges();
            context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId, EmployeeId = employeeId,
                ForPeriod = new DateTime(2024, 6, 30), InPeriod = new DateTime(2024, 6, 30),
                PayrollRunType = PayrollRunType.OffCycle, PayrollReasonTypeId = reason.Id,
                OffCyclePaymentDate = LastDay
            });
        });
    }

    #endregion

    #region << Side effects >>

    /// <summary>The bridge to payroll: saving writes the gratuity row that wage code 4005 sums.</summary>
    [Fact]
    public async Task Should_Write_The_Gratuity_Row_The_End_Of_Service_Payroll_Pays_From()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Resignation);

        await _service.ApplyAsync(Values(seeded.EmployeeId, gratuity: 30.021), null);

        var gratuity = UsingDbContext(context => context.Set<EmployeeGratuity>().Single(g => g.EmployeeId == seeded.EmployeeId));
        gratuity.GratuityPaidDate.ShouldBe(LastDay);
        gratuity.GratuityPaidDays.ShouldBe(30.021);
        gratuity.Comments.ShouldBe(FullAndFinalSettlementService.EndOfServiceComment);
    }

    [Fact]
    public async Task Should_Encash_Leave_And_Tickets_And_Make_The_Employee_Inactive()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedLeaveBalance(seeded.EmployeeId, 30);
        SeedTicketBalance(seeded.EmployeeId, 1);

        var values = Values(seeded.EmployeeId, gratuity: 0);
        values.LeaveCompensateDays = 12;
        values.TicketCompensateCount = 1;
        await _service.ApplyAsync(values, null);

        UsingDbContext(context =>
        {
            var leave = context.Set<EmployeeAbsAdjustment>().Single(a => a.EmployeeId == seeded.EmployeeId);
            leave.AdjustmentType.ShouldBe(AdjustmentType.Encashment);
            leave.QuotaAdjusted.ShouldBe(12);
            leave.EffectiveDate.ShouldBe(LastDay);

            var ticket = context.Set<EmployeeTktAdjustment>().Single(a => a.EmployeeId == seeded.EmployeeId);
            ticket.QuotaAdjusted.ShouldBe(1);

            context.Set<Employee>().Find(seeded.EmployeeId).Status.ShouldBe(EmployeeStatus.Inactive);
        });
    }

    /// <summary>
    /// Moving the compensation date must MOVE the gratuity row, not add a second one -- otherwise the
    /// end-of-service off-cycle would pay both.
    /// </summary>
    [Fact]
    public async Task Should_Move_Not_Duplicate_The_Gratuity_Row_On_Edit()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        var previous = Values(seeded.EmployeeId, gratuity: 60);
        await _service.ApplyAsync(previous, null);

        var current = Values(seeded.EmployeeId, gratuity: 55);
        current.CompensationDate = new DateTime(2024, 7, 15);
        await _service.ApplyAsync(current, previous);

        var rows = UsingDbContext(context => context.Set<EmployeeGratuity>().Where(g => g.EmployeeId == seeded.EmployeeId && !g.IsDeleted).ToList());
        rows.Count.ShouldBe(1);
        rows[0].GratuityPaidDate.ShouldBe(new DateTime(2024, 7, 15));
        rows[0].GratuityPaidDays.ShouldBe(55);
    }

    [Fact]
    public async Task Should_Undo_Everything_And_Reactivate_The_Employee_On_Delete()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedLeaveBalance(seeded.EmployeeId, 30);
        var values = Values(seeded.EmployeeId, gratuity: 60);
        values.LeaveCompensateDays = 12;
        await _service.ApplyAsync(values, null);

        await _service.UndoAsync(values);

        UsingDbContext(context =>
        {
            context.Set<EmployeeGratuity>().Any(g => g.EmployeeId == seeded.EmployeeId && !g.IsDeleted).ShouldBeFalse();
            context.Set<EmployeeAbsAdjustment>().Any(a => a.EmployeeId == seeded.EmployeeId && !a.IsDeleted).ShouldBeFalse();
            context.Set<Employee>().Find(seeded.EmployeeId).Status.ShouldBe(EmployeeStatus.Active);
        });
    }

    [Fact]
    public async Task Should_Record_The_Indemnity_Pay_Adjustment_As_A_Payment()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        var wageTypeId = UsingDbContext(context =>
        {
            var wage = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId, Code = "1011", NameEn = "Indemnity Pay Adjust", NameAr = "تعديل", PayrollOrder = 1, Status = true
            }).Entity;
            context.SaveChanges();
            return wage.Id;
        });

        var values = Values(seeded.EmployeeId, gratuity: 0);
        values.IndemnityPayAdjustment = 125.5;
        await _service.ApplyAsync(values, null);

        var payment = UsingDbContext(context => context.Set<EmployeePayment>().Single(p => p.EmployeeId == seeded.EmployeeId && p.WageTypeId == wageTypeId));
        payment.Amount.ShouldBe(125.5);
        payment.PaymentDate.ShouldBe(LastDay);
        payment.IsSystemGenerated.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Fail_An_Indemnity_Pay_Adjustment_When_Its_Wage_Type_Is_Missing()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        var values = Values(seeded.EmployeeId, gratuity: 0);
        values.IndemnityPayAdjustment = 10;

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => _service.ApplyAsync(values, null));

        ex.Message.ShouldBe("WageType : 'Indemnity Pay Adjust' not found.");
    }

    #endregion

    private static FullAndFinalValues Values(int employeeId, double gratuity)
    {
        return new FullAndFinalValues
        {
            EmployeeId = employeeId,
            CompensationDate = LastDay,
            GratuityCompensateDays = gratuity
        };
    }
}
