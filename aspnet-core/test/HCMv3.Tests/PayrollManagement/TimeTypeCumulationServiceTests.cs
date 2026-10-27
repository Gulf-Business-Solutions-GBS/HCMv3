using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Tests for <see cref="TimeTypeCumulationService"/> -- the port of legacy
/// <c>HCM10.PayrollEngine/Program.cs</c> lines 1956-2518 + <c>CalculateTimeType</c> (2717).
///
/// <para>Priorities, per the wave brief: (1) the 4xxx-vs-5xxx split -- proving 5xxx is <c>GetArrear</c> over
/// OTHER registers and NOT a re-run of the 4xxx maths; (2) the <c>tempNumber != 0</c> suppression, including
/// that it does not clear an existing line; (3) the per-day breakdown rows.</para>
/// </summary>
public class TimeTypeCumulationServiceTests
{
    private const int TenantId = 1;
    private const int EmployeeId = 7;

    #region << CalculateTimeType writer (legacy line 2717) >>

    [Fact]
    public async Task CalculateTimeType_Should_Write_Nothing_When_The_Number_Is_Zero()
    {
        // Legacy line 2719: `if (tempNumber != 0)` wraps the WHOLE body.
        var harness = new Harness();

        await harness.WriteAsync("4011", "Active Days", 0);

        harness.Store.Lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task CalculateTimeType_Should_Leave_An_Existing_Line_Untouched_When_The_Number_Is_Zero()
    {
        // The suppression is NOT "write 0" and NOT "delete": legacy simply never enters the block, so a
        // previously-posted number survives.
        var harness = new Harness();
        await harness.WriteAsync("4011", "Active Days", 12);

        await harness.WriteAsync("4011", "Active Days", 0);

        harness.Store.Lines.Single(l => l.WageCode == "4011").Number.ShouldBe(12);
    }

    [Fact]
    public async Task CalculateTimeType_Should_Preserve_Rate_And_Amount_On_An_Existing_Line()
    {
        // Legacy lines 2730-2731 (`Rate = 0` / `Amount = 0`) are COMMENTED OUT -- only Number is assigned.
        var harness = new Harness();
        harness.Store.Lines.Add(new PayrollRegisterLine
        {
            Id = 99,
            WageTypeId = harness.WageTypeId("4011"),
            WageCode = "4011",
            Rate = 25.5,
            Number = 1,
            Amount = 300
        });

        await harness.WriteAsync("4011", "Active Days", 21);

        var line = harness.Store.Lines.Single();
        line.Number.ShouldBe(21);
        line.Rate.ShouldBe(25.5);
        line.Amount.ShouldBe(300);
    }

    [Fact]
    public async Task CalculateTimeType_Should_Not_Delete_The_Line_Even_Though_Its_Amount_Is_Zero()
    {
        // Time-type lines carry Number only, so the store's "zero amount means delete" rule must be off
        // (deleteWhenAmountIsZero: false). Without that flag every day-count row would vanish on write.
        var harness = new Harness();

        await harness.WriteAsync("4011", "Active Days", 30);

        harness.Store.Lines.Single().Amount.ShouldBe(0);
        harness.Store.DeletedLineIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task CalculateTimeType_Should_Throw_With_The_Legacy_Message_When_The_WageType_Is_Missing()
    {
        // Legacy line 2728: "WageType " + WageCode + " : " + message + " not found".
        var harness = new Harness();

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => harness.WriteAsync("9999", "Made Up Days", 5));

        ex.Message.ShouldBe("WageType 9999 : Made Up Days not found");
    }

    [Fact]
    public async Task CalculateTimeType_Should_Not_Throw_For_A_Missing_WageType_When_The_Number_Is_Zero()
    {
        // The zero-guard runs BEFORE the wage-type lookup (legacy 2719 precedes 2721-2728), so an unmapped
        // code with nothing to post is silently fine.
        var harness = new Harness();

        await harness.WriteAsync("9999", "Made Up Days", 0);

        harness.Store.Lines.ShouldBeEmpty();
    }

    #endregion

    #region << Per-day breakdown rows (legacy 2742-2745 -> AddSalaryRegisterDaysDetail, 5439) >>

    [Fact]
    public async Task CalculateTimeType_Should_Append_The_PerDay_Breakdown_Rows()
    {
        var harness = new Harness();

        await harness.WriteAsync("4000", "Annual Leave Paid Days", 9, new List<SalaryRegisterDayCount>
        {
            new SalaryRegisterDayCount { Period = new DateTime(2026, 1, 31), Number = 4 },
            new SalaryRegisterDayCount { Period = new DateTime(2026, 2, 28), Number = 5 }
        });

        var lineId = harness.Store.Lines.Single().Id;
        harness.Store.DayDetails[lineId].Select(d => d.Number).ShouldBe(new[] { 4d, 5d });
    }

    [Fact]
    public async Task CalculateTimeType_Should_Skip_AllZero_Breakdown_Rows()
    {
        // Legacy AddSalaryRegisterDaysDetail line 5443: `if (rate != 0 || number != 0 || amount != 0)`.
        var harness = new Harness();

        await harness.WriteAsync("4000", "Annual Leave Paid Days", 4, new List<SalaryRegisterDayCount>
        {
            new SalaryRegisterDayCount { Period = new DateTime(2026, 1, 31), Number = 4 },
            new SalaryRegisterDayCount { Period = new DateTime(2026, 2, 28), Number = 0 }
        });

        var lineId = harness.Store.Lines.Single().Id;
        harness.Store.DayDetails[lineId].Count.ShouldBe(1);
        harness.Store.DayDetails[lineId][0].Period.ShouldBe(new DateTime(2026, 1, 31));
    }

    [Fact]
    public async Task CalculateTimeType_Should_Not_Replace_The_Breakdown_When_No_Rows_Were_Produced()
    {
        // Legacy loops over an empty list and therefore touches nothing. SaveDayDetailsAsync REPLACES, so it
        // must not be called at all -- otherwise a regular run would wipe an off-cycle run's breakdown.
        var harness = new Harness();

        await harness.WriteAsync("4011", "Active Days", 30, new List<SalaryRegisterDayCount>());

        harness.Store.DayDetails.ShouldBeEmpty();
    }

    #endregion

    #region << 4xxx vs 5xxx: the arrear split (legacy GetArrear, line 5302) >>

    [Fact]
    public async Task Arrear_Should_Be_Zero_When_There_Are_No_Other_Retro_Registers()
    {
        // Legacy line 5307's second condition: `currentSalRegisters.Count != 0`.
        var harness = new Harness();
        var context = harness.Context();

        var arrear = await harness.ArrearAsync(context, "4011");

        arrear.ShouldBe(0);
    }

    [Fact]
    public async Task Arrear_Should_Be_Zero_On_A_Retro_Period_Because_PayDate_Is_Not_PayInDate()
    {
        // Legacy line 5307's first condition: `payDate == payInDate`. Only the CURRENT period's register
        // carries the arrear roll-up; the retro registers themselves must not.
        var harness = new Harness();
        var context = harness.Context();
        context.PayDate = new DateTime(2026, 1, 31);   // retro period
        context.PayInDate = new DateTime(2026, 3, 31); // paid in March
        context.CurrentSalaryRegisters.Add(harness.Register(id: 20, forPeriod: new DateTime(2026, 1, 31)));

        var arrear = await harness.ArrearAsync(context, "4011");

        arrear.ShouldBe(0);
    }

    [Fact]
    public async Task Arrear_Should_Sum_The_Retro_Registers_Number_For_A_TimeType_WageType()
    {
        // The heart of the 4xxx/5xxx distinction: 5011 is NOT "GetActiveDays over a different window", it is
        // the retro register's ALREADY-POSTED 4011 Number, minus what was previously paid for that period.
        var harness = new Harness();
        var context = harness.Context();

        var retro = harness.Register(id: 20, forPeriod: new DateTime(2026, 1, 31), inPeriod: new DateTime(2026, 3, 31));
        context.CurrentSalaryRegisters.Add(retro);
        harness.SetLine(registerId: 20, wageCode: "4011", number: 30);

        var arrear = await harness.ArrearAsync(context, "4011");

        arrear.ShouldBe(30);
    }

    [Fact]
    public async Task Arrear_Should_Subtract_What_Was_Already_Paid_For_The_Same_Period()
    {
        // Legacy lines 5322-5333: find the latest EARLIER-paid REGULAR register for the same ForPeriod and
        // subtract its figure, so only the DELTA is paid again.
        var harness = new Harness();
        var context = harness.Context();

        var retro = harness.Register(id: 20, forPeriod: new DateTime(2026, 1, 31), inPeriod: new DateTime(2026, 3, 31));
        context.CurrentSalaryRegisters.Add(retro);
        harness.SetLine(registerId: 20, wageCode: "4011", number: 30);

        // The originally-paid January register (InPeriod < payInDate, Regular).
        var original = harness.Register(id: 10, forPeriod: new DateTime(2026, 1, 31), inPeriod: new DateTime(2026, 1, 31));
        harness.SalaryRegisters.Add(original);
        harness.SetLine(registerId: 10, wageCode: "4011", number: 28);

        var arrear = await harness.ArrearAsync(context, "4011");

        arrear.ShouldBe(2);
    }

    [Fact]
    public async Task Arrear_Should_Use_Amount_Rather_Than_Number_For_A_NonTimeType_WageType()
    {
        // Legacy line 5320's ternary keys off BillingType.
        var harness = new Harness();
        var context = harness.Context();

        var retro = harness.Register(id: 20, forPeriod: new DateTime(2026, 1, 31), inPeriod: new DateTime(2026, 3, 31));
        context.CurrentSalaryRegisters.Add(retro);
        harness.SetLine(registerId: 20, wageCode: "1000", number: 30, amount: 450);

        var arrear = await harness.ArrearAsync(context, "1000");

        arrear.ShouldBe(450);
    }

    [Fact]
    public async Task Arrear_Should_Treat_A_Missing_Line_As_Zero_Rather_Than_Skipping_The_Register()
    {
        // Legacy line 5318: the `continue` was deliberately COMMENTED OUT so that a DELETED annual leave
        // (line present before, absent now) still produces a negative arrear.
        var harness = new Harness();
        var context = harness.Context();

        var retro = harness.Register(id: 20, forPeriod: new DateTime(2026, 1, 31), inPeriod: new DateTime(2026, 3, 31));
        context.CurrentSalaryRegisters.Add(retro);
        // No 4000 line on the retro register at all.

        var original = harness.Register(id: 10, forPeriod: new DateTime(2026, 1, 31), inPeriod: new DateTime(2026, 1, 31));
        harness.SalaryRegisters.Add(original);
        harness.SetLine(registerId: 10, wageCode: "4000", number: 5);

        var arrear = await harness.ArrearAsync(context, "4000");

        arrear.ShouldBe(-5);
    }

    #endregion

    #region << GetSalaryRegisterDaysList (legacy line 5387) >>

    [Fact]
    public void SalaryRegisterDaysList_Should_Be_Empty_On_A_Regular_Run()
    {
        // Legacy line 5392 gates the whole loop on `payrollRunType == OffCycle`.
        var harness = new Harness();
        var context = harness.Context();
        context.PayrollRunType = PayrollRunType.Regular;

        var days = harness.DaysList(context, new DateTime(2026, 1, 1), new DateTime(2026, 2, 15), SalaryRegisterDayType.AnnualLeaveDay);

        days.ShouldBeEmpty();
    }

    [Fact]
    public void SalaryRegisterDaysList_Should_Emit_One_Row_Per_Calendar_Month_Stamped_With_The_Month_End()
    {
        var harness = new Harness();
        var context = harness.Context();
        context.PayrollRunType = PayrollRunType.OffCycle;

        harness.TimeRegisters.Add(TimeReg(new DateTime(2026, 1, 20), annualLeaveDay: 3));
        harness.TimeRegisters.Add(TimeReg(new DateTime(2026, 2, 10), annualLeaveDay: 2));

        var days = harness.DaysList(context, new DateTime(2026, 1, 1), new DateTime(2026, 2, 15), SalaryRegisterDayType.AnnualLeaveDay);

        days.Count.ShouldBe(2);
        days[0].Period.ShouldBe(new DateTime(2026, 1, 31));
        days[0].Number.ShouldBe(3);
        days[1].Period.ShouldBe(new DateTime(2026, 2, 28));
        days[1].Number.ShouldBe(2);
    }

    [Fact]
    public void SalaryRegisterDaysList_Should_Add_LeaveAdjDay_To_AnnualLeaveDay()
    {
        // Legacy line 5410: `r.AnnualLeaveDay + (r.LeaveAdjDay == null ? 0 : r.LeaveAdjDay)`.
        var harness = new Harness();
        var context = harness.Context();
        context.PayrollRunType = PayrollRunType.OffCycle;

        harness.TimeRegisters.Add(TimeReg(new DateTime(2026, 1, 20), annualLeaveDay: 3, leaveAdjDay: 0.5));

        var days = harness.DaysList(context, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), SalaryRegisterDayType.AnnualLeaveDay);

        days.Single().Number.ShouldBe(3.5);
    }

    [Fact]
    public void SalaryRegisterDaysList_Should_Exclude_Absent_Days_From_The_RestDay_Breakdown()
    {
        // Legacy line 5407: rest days that turned into absences are not paid as rest days.
        var harness = new Harness();
        var context = harness.Context();
        context.PayrollRunType = PayrollRunType.OffCycle;

        harness.TimeRegisters.Add(TimeReg(new DateTime(2026, 1, 10), restDay: 1));
        harness.TimeRegisters.Add(TimeReg(new DateTime(2026, 1, 11), restDay: 1, absentDay: 1));

        var days = harness.DaysList(context, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), SalaryRegisterDayType.RestDay);

        days.Single().Number.ShouldBe(1);
    }

    #endregion

    #region << 4021 Annual Leave Opening Balance -- June-2024 production parity (defect 4) >>

    // These four tests pin the fix for the June-2024 HRMS_RCF-vs-HCMv3 parity gap on wage type 4021.
    //
    // MECHANISM (legacy Program.cs 3160-3179): when neither the previous month's own register nor a later
    // retro register for that ForPeriod exists, GetAnnualLeaveOpeningBalance recomputes from history as
    //     totalAccrual - (totalUsed + totalAdjusted + totalForfeit)
    // where totalAdjusted (line 3170) is the sum of EmployeeAbsAdjustment.QuotaAdjusted rows matching
    //     AbsenceType.Oid == AnnualLeaveOid || AbsenceType.QuotaType == QuotaType.AnnualLeave
    // and AnnualLeaveOid is resolved at legacy line 2710 as the AbsenceType whose Code is the string "1".
    //
    // THE BUG: this port read PayrollEngineSettings.AnnualLeaveAbsenceTypeId, which is declared and read but
    // NEVER ASSIGNED in production code, so it was permanently 0. The first arm therefore never matched, and
    // because the production annual-leave AbsenceType rows carry QuotaType.None the second arm never matched
    // either -- totalAdjusted collapsed to 0 and every opening balance came out too HIGH by exactly the
    // employee's outstanding adjustment total.

    [Fact]
    public async Task GetAnnualLeaveOpeningBalance_Should_Subtract_Annual_Leave_Quota_Adjustments_When_Recomputed_From_History()
    {
        // Employee code 2362, June-2024 (HRMS_RCF register 55919/57209/58433 all agree on 4021 = 14.045997).
        // History to 2024-05-31: accrual 139.045997, used 113, one 12.000-day adjustment (2022-01-25).
        // Before the fix this returned 26.045997 -- exactly +12.000.
        var harness = new Harness();
        harness.SeedOpeningBalanceHistory(totalAccrual: 139.045997, totalUsed: 113);
        harness.AddAnnualLeaveAdjustment(new DateTime(2022, 1, 25), 12);

        var openingBalance = await harness.OpeningBalanceAsync(harness.Context());

        openingBalance.ShouldBe(14.045997, 0.000001);
    }

    [Fact]
    public async Task GetAnnualLeaveOpeningBalance_Should_Subtract_Every_Annual_Leave_Adjustment_Up_To_The_Previous_Month_End()
    {
        // Employee code 3400, June-2024 (HRMS_RCF: 4021 = 13.914995). History to 2024-05-31: accrual
        // 118.914995, used 45, TWO 30.000-day adjustments (2021-11-01 and 2024-01-20). Before the fix this
        // returned 73.914995 -- exactly +60.000.
        var harness = new Harness();
        harness.SeedOpeningBalanceHistory(totalAccrual: 118.914995, totalUsed: 45);
        harness.AddAnnualLeaveAdjustment(new DateTime(2021, 11, 1), 30);
        harness.AddAnnualLeaveAdjustment(new DateTime(2024, 1, 20), 30);

        var openingBalance = await harness.OpeningBalanceAsync(harness.Context());

        openingBalance.ShouldBe(13.914995, 0.000001);
    }

    [Fact]
    public async Task GetAnnualLeaveOpeningBalance_Should_Resolve_The_AnnualLeave_AbsenceType_By_Code_When_The_Settings_Snapshot_Is_Unpopulated()
    {
        // The regression guard proper: Settings.AnnualLeaveAbsenceTypeId is 0 (its production value) AND the
        // AbsenceType carries QuotaType.None, so BOTH arms of legacy's OR fail unless the id is resolved from
        // AbsenceType.Code == "1".
        var harness = new Harness();
        harness.SeedOpeningBalanceHistory(totalAccrual: 100, totalUsed: 0);
        harness.AddAnnualLeaveAdjustment(new DateTime(2024, 1, 20), 30);

        var context = harness.Context();
        context.Settings.AnnualLeaveAbsenceTypeId.ShouldBe(0);
        context.Settings.AnnualLeaveAbsenceTypeId = 0;

        (await harness.OpeningBalanceAsync(context)).ShouldBe(70, 0.000001);
    }

    [Fact]
    public async Task GetAnnualLeaveOpeningBalance_Should_Ignore_Adjustments_For_Other_AbsenceTypes()
    {
        // The fix must not over-reach: an unpaid-leave (code "2", QuotaType.None) adjustment is not an
        // annual-leave adjustment and legacy's predicate excludes it.
        var harness = new Harness();
        harness.SeedOpeningBalanceHistory(totalAccrual: 100, totalUsed: 0);
        harness.AbsAdjustments.Add(new EmployeeAbsAdjustment
        {
            Id = 900,
            TenantId = TenantId,
            EmployeeId = EmployeeId,
            AbsenceTypeId = 42,
            AbsenceType = harness.AbsenceTypes.Single(a => a.Id == 42),
            EffectiveDate = new DateTime(2024, 1, 20),
            QuotaAdjusted = 30
        });

        (await harness.OpeningBalanceAsync(harness.Context())).ShouldBe(100, 0.000001);
    }

    #endregion

    #region << 4023 Total Active Days -- scope pin (defect 2) >>

    [Fact]
    public async Task GetTotalActiveDays_Should_Be_This_Months_ActiveDays_Only_When_There_Is_No_Previous_Register()
    {
        // Legacy GetTotalActiveDays (Program.cs 4923-4951) is a REGISTER ROLL-UP, not a from-scratch history
        // walk: prevRegister.4023 + prevRegister.5023 + thisRegister.4011 + thisRegister.5011. With no
        // previous register it is simply this month's 4011.
        //
        // This is why the June-2024 parity run showed 30 against legacy's 1187: HRMS_RCF has an unbroken
        // chain of registers back to 2021-04, HCMv3 has exactly one. 1187 is precisely the number of days
        // from 2021-04-01 to 2024-06-30 inclusive, i.e. the accumulated monthly 4011 values. It is NOT a
        // scope defect in this method.
        var harness = new Harness();
        var context = harness.Context();
        harness.SetLine(context.Register.RegisterId, "4011", number: 30);

        (await harness.TotalActiveDaysAsync(context)).ShouldBe(30);
    }

    [Fact]
    public async Task GetTotalActiveDays_Should_Accumulate_The_Previous_Registers_Total()
    {
        // The same method, given the chain HRMS_RCF has, does produce the running cumulative counter.
        var harness = new Harness();
        var context = harness.Context();

        var prev = harness.Register(2, new DateTime(2026, 2, 28));
        harness.SalaryRegisters.Add(prev);
        context.PreviousSalaryRegisters = new List<SalaryRegister> { prev };
        harness.SetLine(2, "4011", number: 28);
        harness.Store.LinesByRegister(2).Add(new PayrollRegisterLine { Id = 291, WageCode = "4023", WageTypeId = 291, Number = 1157 });
        harness.SetLine(context.Register.RegisterId, "4011", number: 30);

        (await harness.TotalActiveDaysAsync(context)).ShouldBe(1187);
    }

    #endregion

    #region << Harness >>

    private static TimeRegister TimeReg(
        DateTime dayDate,
        double annualLeaveDay = 0,
        double leaveAdjDay = 0,
        double restDay = 0,
        double absentDay = 0)
    {
        return new TimeRegister
        {
            TenantId = TenantId,
            EmployeeId = EmployeeId,
            DayDate = dayDate,
            AnnualLeaveDay = annualLeaveDay,
            LeaveAdjDay = leaveAdjDay,
            RestDay = restDay,
            AbsentDay = absentDay
        };
    }

    /// <summary>
    /// Wires a <see cref="TimeTypeCumulationService"/> over in-memory fakes and exposes the three units under
    /// test (the writer, the arrear function and the day-list builder) through the public
    /// <see cref="ITimeTypeCumulationService.RunAsync"/> surface's private helpers via a thin subclass.
    /// </summary>
    private sealed class Harness
    {
        public List<WageType> WageTypes { get; } = new List<WageType>
        {
            new WageType { Id = 1, TenantId = TenantId, Code = "4000", NameEn = "Annual Leave Paid Days", BillingType = BillingType.TimeType },
            new WageType { Id = 2, TenantId = TenantId, Code = "4011", NameEn = "Active Days", BillingType = BillingType.TimeType },
            new WageType { Id = 3, TenantId = TenantId, Code = "5011", NameEn = "Active Days Arrear", BillingType = BillingType.TimeType },
            new WageType { Id = 4, TenantId = TenantId, Code = "1000", NameEn = "Basic Pay", BillingType = BillingType.Earning }
        };

        public List<SalaryRegister> SalaryRegisters { get; } = new List<SalaryRegister>();

        public List<TimeRegister> TimeRegisters { get; } = new List<TimeRegister>();

        /// <summary>
        /// Mirrors production: the annual-leave <c>AbsenceType</c> is identified by <c>Code == "1"</c> and
        /// carries <c>QuotaType.None</c> -- so a rule that relies on the <c>QuotaType.AnnualLeave</c> arm
        /// alone matches nothing.
        /// </summary>
        public List<AbsenceType> AbsenceTypes { get; } = new List<AbsenceType>
        {
            new AbsenceType { Id = 41, TenantId = TenantId, Code = "1", NameEn = "Annual Paid Leave", QuotaType = QuotaType.None },
            new AbsenceType { Id = 42, TenantId = TenantId, Code = "2", NameEn = "Unpaid Leave", QuotaType = QuotaType.None }
        };

        public List<EmployeeAbsAdjustment> AbsAdjustments { get; } = new List<EmployeeAbsAdjustment>();

        public List<EmployeeAbsQuotaForfeit> AbsQuotaForfeits { get; } = new List<EmployeeAbsQuotaForfeit>();

        public FakeRegisterAwareStore Store { get; } = new FakeRegisterAwareStore();

        private TestableService _service;

        /// <summary>
        /// Built LAZILY on first use. <c>PayrollEngineFakes.Repo</c> snapshots the list it is handed
        /// (<c>items?.ToList()</c>), so any row a test adds to <see cref="SalaryRegisters"/> after
        /// construction would be invisible to the service. Deferring construction to the first call lets
        /// tests arrange their data in the natural order.
        /// </summary>
        private TestableService Service => _service ??= Build();

        private TestableService Build()
        {
            return new TestableService(
                PayrollEngineFakes.Repo(WageTypes),
                PayrollEngineFakes.Repo(SalaryRegisters),
                PayrollEngineFakes.Repo(new List<TimeTransfer>()),
                PayrollEngineFakes.Repo(AbsAdjustments),
                PayrollEngineFakes.Repo(AbsQuotaForfeits),
                PayrollEngineFakes.Repo(new List<EmployeeAbsence>()),
                PayrollEngineFakes.Repo(new List<EmployeeGratuity>()),
                PayrollEngineFakes.Repo(new List<EmployeeSeparation>()),
                PayrollEngineFakes.Repo(new List<EmployeeContract>()),
                PayrollEngineFakes.Repo(AbsenceTypes),
                Store,
                Substitute.For<IPayrollDayCountService>(),
                Substitute.For<IAccrualService>(),
                Substitute.For<IIndemnityCalculationService>());
        }

        public int WageTypeId(string code) => WageTypes.Single(w => w.Code == code).Id;

        public SalaryRegister Register(int id, DateTime forPeriod, DateTime? inPeriod = null)
        {
            return new SalaryRegister
            {
                Id = id,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                ForPeriod = forPeriod,
                InPeriod = inPeriod ?? forPeriod,
                PayrollRunType = PayrollRunType.Regular
            };
        }

        /// <summary>
        /// Seeds the two TimeRegister aggregates legacy's from-scratch opening-balance fallback reads
        /// (lines 3162-3168): the lifetime <c>AnnualLeaveAccrualPerDay</c> total and the lifetime
        /// <c>AnnualLeaveDay + LeaveAdjDay</c> total, both up to the previous month end. Carried on a single
        /// dated row apiece -- the fallback only ever sums them.
        /// </summary>
        public void SeedOpeningBalanceHistory(double totalAccrual, double totalUsed)
        {
            TimeRegisters.Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                DayDate = new DateTime(2026, 2, 27),
                AnnualLeaveAccrualPerDay = totalAccrual
            });

            TimeRegisters.Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                DayDate = new DateTime(2026, 2, 28),
                AnnualLeaveDay = totalUsed
            });
        }

        public void AddAnnualLeaveAdjustment(DateTime effectiveDate, double quotaAdjusted)
        {
            AbsAdjustments.Add(new EmployeeAbsAdjustment
            {
                Id = AbsAdjustments.Count + 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                AbsenceTypeId = 41,
                AbsenceType = AbsenceTypes.Single(a => a.Id == 41),
                EffectiveDate = effectiveDate,
                QuotaAdjusted = quotaAdjusted
            });
        }

        public void SetLine(int registerId, string wageCode, double number = 0, double amount = 0)
        {
            Store.LinesByRegister(registerId).Add(new PayrollRegisterLine
            {
                Id = registerId * 100 + WageTypeId(wageCode),
                WageTypeId = WageTypeId(wageCode),
                WageCode = wageCode,
                Number = number,
                Amount = amount
            });
        }

        public PayrollCalculationContext Context()
        {
            var payDate = new DateTime(2026, 3, 31);

            return new PayrollCalculationContext
            {
                EmployeeId = EmployeeId,
                PayDate = payDate,
                PayStartDate = new DateTime(2026, 3, 1),
                PayInDate = payDate,
                OffCycleEndDate = payDate,
                PayrollRunType = PayrollRunType.Regular,
                Register = new PayrollRegisterRef
                {
                    RegisterId = 1,
                    Target = PayrollRegisterTarget.Live,
                    ForPeriod = payDate,
                    InPeriod = payDate,
                    PayrollRunType = PayrollRunType.Regular
                },
                TimeRegisterList = TimeRegisters,
                TimeRegisterQuery = TimeRegisters.AsQueryable(),
                Settings = new PayrollEngineSettings { ForfeitRule = ForfeitRule.None }
            };
        }

        public Task WriteAsync(string wageCode, string message, double number, List<SalaryRegisterDayCount> days = null)
        {
            return Service.WriteAsync(Context().Register, wageCode, message, number, days);
        }

        public Task<double> ArrearAsync(PayrollCalculationContext context, string wageCode)
            => Service.ArrearAsync(context, wageCode);

        public List<SalaryRegisterDayCount> DaysList(PayrollCalculationContext context, DateTime start, DateTime end, SalaryRegisterDayType dayType)
            => Service.DaysList(context, start, end, dayType);

        public Task<double> OpeningBalanceAsync(PayrollCalculationContext context)
            => Service.OpeningBalanceAsync(context);

        public Task<double> TotalActiveDaysAsync(PayrollCalculationContext context)
            => Service.TotalActiveDaysAsync(context);
    }

    /// <summary>
    /// Exposes the three private units under test. They are private on the production type because they are
    /// implementation detail of <see cref="ITimeTypeCumulationService.RunAsync"/>; testing them directly is
    /// what makes the 63 legacy branches tractable.
    /// </summary>
    private sealed class TestableService : TimeTypeCumulationService
    {
        public TestableService(
            IRepository<WageType> wageTypeRepository,
            IRepository<SalaryRegister> salaryRegisterRepository,
            IRepository<TimeTransfer> timeTransferRepository,
            IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
            IRepository<EmployeeAbsQuotaForfeit> employeeAbsQuotaForfeitRepository,
            IRepository<EmployeeAbsence> employeeAbsenceRepository,
            IRepository<EmployeeGratuity> employeeGratuityRepository,
            IRepository<EmployeeSeparation> employeeSeparationRepository,
            IRepository<EmployeeContract> employeeContractRepository,
            IRepository<AbsenceType> absenceTypeRepository,
            IPayrollRegisterStore registerStore,
            IPayrollDayCountService dayCountService,
            IAccrualService accrualService,
            IIndemnityCalculationService indemnityService)
            : base(wageTypeRepository, salaryRegisterRepository, timeTransferRepository,
                employeeAbsAdjustmentRepository, employeeAbsQuotaForfeitRepository, employeeAbsenceRepository,
                employeeGratuityRepository, employeeSeparationRepository, employeeContractRepository,
                absenceTypeRepository, registerStore, dayCountService, accrualService, indemnityService)
        {
        }

        public Task WriteAsync(PayrollRegisterRef register, string wageCode, string message, double number, List<SalaryRegisterDayCount> days)
            => CalculateTimeTypeAsync(register, wageCode, message, number, days);

        public Task<double> ArrearAsync(PayrollCalculationContext context, string wageCode)
            => GetArrearAsync(context, wageCode);

        public List<SalaryRegisterDayCount> DaysList(PayrollCalculationContext context, DateTime start, DateTime end, SalaryRegisterDayType dayType)
            => GetSalaryRegisterDaysList(context, start, end, dayType);

        public Task<double> OpeningBalanceAsync(PayrollCalculationContext context)
            => GetAnnualLeaveOpeningBalanceAsync(context);

        public Task<double> TotalActiveDaysAsync(PayrollCalculationContext context)
            => GetTotalActiveDaysAsync(context);
    }

    /// <summary>
    /// Register-AWARE in-memory store. <see cref="FakePayrollRegisterStore"/> deliberately ignores the
    /// register argument (one register per test), but the arrear tests read THREE registers at once -- the
    /// current one, the retro sibling and the originally-paid one -- so they need lines keyed by register id.
    /// </summary>
    internal sealed class FakeRegisterAwareStore : IPayrollRegisterStore
    {
        private readonly Dictionary<int, List<PayrollRegisterLine>> _byRegister = new Dictionary<int, List<PayrollRegisterLine>>();
        private int _nextId = 1;

        /// <summary>The default register (id 1) -- what the writer tests assert against.</summary>
        public List<PayrollRegisterLine> Lines => LinesByRegister(1);

        public Dictionary<int, List<SalaryRegisterDayCount>> DayDetails { get; } = new Dictionary<int, List<SalaryRegisterDayCount>>();

        public List<int> DeletedLineIds { get; } = new List<int>();

        public List<PayrollRegisterLine> LinesByRegister(int registerId)
        {
            if (!_byRegister.TryGetValue(registerId, out var lines))
            {
                lines = new List<PayrollRegisterLine>();
                _byRegister[registerId] = lines;
            }

            return lines;
        }

        public Task<List<PayrollRegisterLine>> GetLinesAsync(PayrollRegisterRef register)
            => Task.FromResult(LinesByRegister(register.RegisterId).ToList());

        public Task<PayrollRegisterLine> GetLineAsync(PayrollRegisterRef register, string wageCode)
            => Task.FromResult(LinesByRegister(register.RegisterId).FirstOrDefault(l => l.WageCode == wageCode));

        public Task<PayrollRegisterLine> GetLineByWageTypeIdAsync(PayrollRegisterRef register, int wageTypeId)
            => Task.FromResult(LinesByRegister(register.RegisterId).FirstOrDefault(l => l.WageTypeId == wageTypeId));

        public Task<PayrollRegisterLine> SaveLineAsync(PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero = true)
        {
            var lines = LinesByRegister(register.RegisterId);
            var existing = lines.FirstOrDefault(l => l.WageTypeId == line.WageTypeId && l.WageTypeId != 0)
                           ?? lines.FirstOrDefault(l => l.WageCode == line.WageCode && line.WageCode != null);

            if (deleteWhenAmountIsZero && line.Amount == 0)
            {
                if (existing != null)
                {
                    DeletedLineIds.Add(existing.Id);
                    lines.Remove(existing);
                }

                return Task.FromResult<PayrollRegisterLine>(null);
            }

            if (existing == null)
            {
                existing = new PayrollRegisterLine
                {
                    Id = _nextId++,
                    WageTypeId = line.WageTypeId,
                    WageCode = line.WageCode
                };
                lines.Add(existing);
            }

            existing.Rate = line.Rate;
            existing.Number = line.Number;
            existing.Amount = line.Amount;
            existing.IsSystemGenerated = true;

            line.Id = existing.Id;
            line.IsSystemGenerated = true;
            return Task.FromResult(line);
        }

        public Task DeleteLineAsync(PayrollRegisterRef register, int wageTypeId)
        {
            var lines = LinesByRegister(register.RegisterId);
            var existing = lines.FirstOrDefault(l => l.WageTypeId == wageTypeId);
            if (existing != null)
            {
                DeletedLineIds.Add(existing.Id);
                lines.Remove(existing);
            }

            return Task.CompletedTask;
        }

        public Task ClearLinesAsync(PayrollRegisterRef register)
        {
            LinesByRegister(register.RegisterId).Clear();
            return Task.CompletedTask;
        }

        public Task SaveDayDetailsAsync(PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days)
        {
            DayDetails[lineId] = days?.ToList() ?? new List<SalaryRegisterDayCount>();
            return Task.CompletedTask;
        }
    }

    #endregion
}
