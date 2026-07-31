using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="ITimeTypeCumulationService"/>
/// <remarks>
/// <para><b>Ported from</b> legacy <c>HCM10.PayrollEngine/Program.cs</c> lines 1956-2518 (the driver),
/// <c>CalculateTimeType</c> (2717), <c>GetSalaryRegisterDaysList</c> (5387),
/// <c>AddSalaryRegisterDaysDetail</c> (5439), <c>GetArrear</c> (5302), and the 22 <c>Get*</c> calculators
/// listed in the interface's reconciliation table.</para>
///
/// <para><b>DELIBERATE DEVIATIONS -- read before extending.</b></para>
/// <list type="number">
/// <item><description><b>Legacy's process-wide static caches are replaced by per-call repository queries.</b>
/// <c>empTimeRegisterList</c> -&gt; <see cref="PayrollCalculationContext.TimeRegisterList"/> (still
/// <c>null</c> on off-cycle runs, legacy line 1771 -- every Regular branch here null-guards it exactly where
/// legacy relied on it being non-null); <c>empTimeRegister</c> -&gt;
/// <see cref="PayrollCalculationContext.TimeRegisterQuery"/>; <c>prevSalRegList</c> -&gt;
/// <see cref="PayrollCalculationContext.PreviousSalaryRegisters"/>; <c>currentSalRegisters</c> -&gt;
/// <see cref="PayrollCalculationContext.CurrentSalaryRegisters"/>; <c>empSalaryRegister</c>,
/// <c>empIndemnityList</c>, <c>empAbsAdjustmentList</c>, <c>empAbsQuotaForfeitList</c>,
/// <c>empTimeTransferList</c>, <c>empContractList</c>, <c>WageTypeList</c> -&gt; injected repositories
/// filtered by <c>EmployeeId</c>.</description></item>
/// <item><description><b>Legacy's <c>catch (Exception ex) { throw ex; }</c> wrappers are dropped</b> --
/// they only destroyed the stack trace. The single EXCEPTION is
/// <c>GetSalaryRegisterDaysList</c>, whose legacy catch is EMPTY (it swallows and returns the
/// partially-built list, legacy lines 5433-5436). That swallow IS preserved, because it changes the
/// result.</description></item>
/// <item><description><b><c>Program.calculateSalRegDaysDetail</c> is a hardcoded <c>true</c> literal</b>
/// (legacy line 131, never re-assigned -- same finding <see cref="PresentDaysResult"/> records), so the
/// day-breakdown gate collapses to "off-cycle run only".</description></item>
/// <item><description><b><c>TimeRegister.LeaveTypeId</c> is a bare id with no navigation property</b> in this
/// port. Legacy's <c>r.LeaveType.Code == "9"</c> (Lieu) and <c>== "15"</c> (Unapproved Absence) become an
/// <c>AbsenceType.Code</c> -&gt; id lookup first, then <c>r.LeaveTypeId == id</c> -- the identical
/// translation <c>AccrualService.GetTotalUnpaidLeaveDays</c> already uses (line 1467). When no such
/// AbsenceType row exists the id is <c>null</c> and the predicate matches nothing, which is legacy's
/// behaviour too (its <c>r.LeaveType != null &amp;&amp; r.LeaveType.Code == "9"</c> also matched
/// nothing).</description></item>
/// <item><description><b><c>indemnityPayRuleId</c> is always <c>null</c>.</b>
/// <see cref="IAccrualService.GetTotalIndemnityDaysAsync"/> and
/// <see cref="IIndemnityCalculationService.GetIndemnityDaysAsync"/> take it, but legacy
/// <c>Program.cs</c> contains ZERO references to <c>IndemnityPayRule</c> (verified by full-file search) --
/// the payroll engine only ever passed the <c>EmployeeGroup</c> arm. Passing <c>null</c> is therefore the
/// faithful port, not a shortcut.</description></item>
/// <item><description><b>No rounding is introduced anywhere.</b> Not one of the 31 legacy calculators in this
/// region calls <c>Math.Round</c>; the values are written raw and rounded later by
/// <see cref="IPayrollRoundingService"/>. Adding rounding here would change money.</description></item>
/// <item><description><b>The eight fully-commented-out legacy blocks are NOT ported</b> (4012/5012, 4013/5013,
/// 4019/5019, 5005, 5006, 5025, 5026, 5031). Porting them would ADD behaviour legacy does not
/// have.</description></item>
/// <item><description><b>No EF Core in <c>HCMv3.Core</c></b> -- plain synchronous LINQ over
/// <c>IRepository&lt;T&gt;.GetAll()</c>, same constraint as <c>AccrualService.cs</c> line ~158.</description></item>
/// </list>
/// </remarks>
[UnitOfWork]
public class TimeTypeCumulationService : HCMv3DomainServiceBase, ITimeTypeCumulationService, ITransientDependency
{
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<EmployeeAbsQuotaForfeit> _employeeAbsQuotaForfeitRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeGratuity> _employeeGratuityRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;

    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollDayCountService _dayCountService;
    private readonly IAccrualService _accrualService;
    private readonly IIndemnityCalculationService _indemnityService;

    public TimeTypeCumulationService(
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
    {
        _wageTypeRepository = wageTypeRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _timeTransferRepository = timeTransferRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _employeeAbsQuotaForfeitRepository = employeeAbsQuotaForfeitRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeGratuityRepository = employeeGratuityRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeContractRepository = employeeContractRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _registerStore = registerStore;
        _dayCountService = dayCountService;
        _accrualService = accrualService;
        _indemnityService = indemnityService;
    }

    #region << Driver (legacy Program.cs lines 1956-2518) >>

    /// <inheritdoc/>
    public virtual async Task RunAsync(PayrollCalculationContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var register = context.Register;
        var isRegular = context.PayrollRunType == PayrollRunType.Regular;
        var isEos = context.PayrollRunType == PayrollRunType.OffCycle && context.IsEndOfService;

        // Legacy's ubiquitous guard: "Regular || (OffCycle && offCycleReasons == endOfServiceOffcycle)".
        var regularOrEos = isRegular || isEos;

        // Legacy's other guard shape, used by the accrual/balance codes: "Regular || (OffCycle &&
        // wageType.IncludeInAdvancePay && offCycleReasons != endOfServiceOffcycle)".
        var notEosOffCycle = context.PayrollRunType == PayrollRunType.OffCycle && !context.IsEndOfService;

        double tempNumber;
        List<SalaryRegisterDayCount> days;

        // --- 4000 Annual Leave Paid Days (legacy 1995-2002). NOTE: legacy's run-type gate is COMMENTED OUT
        //     at line 1998, so this runs unconditionally. Preserved.
        tempNumber = GetAnnualLeaveCurrentMonthPaidDays(context);
        days = GetSalaryRegisterDaysList(context, context.PayStartDate, context.OffCycleEndDate, SalaryRegisterDayType.AnnualLeaveDay);
        await CalculateTimeTypeAsync(register, "4000", "Annual Leave Paid Days", tempNumber, days);

        // --- 5000 (legacy 2004-2010).
        await CalculateArrearAsync(context, regularOrEos, "4000", "5000", "Annual Paid Leave Days Arrear");

        // --- 4001 Absent Unpaid Days (legacy 2012-2018). Gate commented out at line 2015.
        tempNumber = await GetAbsentUnpaidDaysAsync(context);
        await CalculateTimeTypeAsync(register, "4001", "Absent Unpaid Days", tempNumber, null);

        // --- 5001 (legacy 2020-2026).
        await CalculateArrearAsync(context, regularOrEos, "4001", "5001", "Absent Unpaid Days Arrear");

        // --- 4029 Absent Days (legacy 2028-2035). Gate commented out at line 2031.
        tempNumber = await _dayCountService.GetAbsentDaysAsync(
            context.EmployeeId, context.PayStartDate, context.PayDate, context.PayrollRunType,
            context.OffCycleEndDate, "4029", context.PayrollArea?.Id ?? 0);
        days = GetSalaryRegisterDaysList(context, context.PayStartDate, context.OffCycleEndDate, SalaryRegisterDayType.AbsentDay);
        await CalculateTimeTypeAsync(register, "4029", "Absent Days", tempNumber, days);

        // --- 5029 (legacy 2037-2043).
        await CalculateArrearAsync(context, regularOrEos, "4029", "5029", "Absent Days Arrear");

        // --- 4002 Sick Leave PAID Days (legacy 2045-2052). Gate commented out at line 2048.
        tempNumber = await _dayCountService.GetSickLeavePaidDaysAsync(
            context.EmployeeId, context.PayDate, context.PayrollRunType, context.OffCycleEndDate);
        days = GetSalaryRegisterDaysList(context, context.PayStartDate, context.OffCycleEndDate, SalaryRegisterDayType.SickLeavePaidDay);
        await CalculateTimeTypeAsync(register, "4002", "Sick Leave Paid Days", tempNumber, days);

        // --- 5002 (legacy 2054-2060).
        await CalculateArrearAsync(context, regularOrEos, "4002", "5002", "Sick Leave Paid Days Arrear");

        // --- 4003 Sick Leave UNPAID Days (legacy 2062-2072). This one IS gated.
        tempNumber = 0;
        days = null;
        if (regularOrEos)
        {
            tempNumber = await _dayCountService.GetSickLeaveUnpaidDaysAsync(
                context.EmployeeId, context.PayDate, context.PayrollRunType, context.OffCycleEndDate);
            days = GetSalaryRegisterDaysList(context, context.PayStartDate, context.OffCycleEndDate, SalaryRegisterDayType.SickLeaveUnpaidDay);
        }

        await CalculateTimeTypeAsync(register, "4003", "Sick Leave Unpaid Days", tempNumber, days);

        // --- 5003 (legacy 2074-2080).
        await CalculateArrearAsync(context, regularOrEos, "4003", "5003", "Sick Leave Unpaid Days Arrear");

        // --- 4007 Public Holiday Days (legacy 2082-2089). Gate commented out at line 2085.
        tempNumber = await _dayCountService.GetPublicHolidayDaysAsync(
            context.EmployeeId, context.PayDate, context.PayrollRunType, context.OffCycleEndDate);
        days = GetSalaryRegisterDaysList(context, context.PayStartDate, context.OffCycleEndDate, SalaryRegisterDayType.PublicHoliday);
        await CalculateTimeTypeAsync(register, "4007", "Public Holiday Days", tempNumber, days);

        // --- 5007 (legacy 2091-2097).
        await CalculateArrearAsync(context, regularOrEos, "4007", "5007", "Public Holiday Days Arrear");

        // --- 4008 Weekly Off Days (legacy 2099-2105). Ungated in legacy.
        tempNumber = await _dayCountService.GetWeeklyOffDaysAsync(
            context.EmployeeId, context.PayDate, context.PayrollRunType, context.OffCycleEndDate, context.OffCycleReason);
        days = GetSalaryRegisterDaysList(context, context.PayStartDate, context.OffCycleEndDate, SalaryRegisterDayType.WeeklyOff);
        await CalculateTimeTypeAsync(register, "4008", "Weekly Off Days", tempNumber, days);

        // --- 5008 (legacy 2107-2113).
        await CalculateArrearAsync(context, regularOrEos, "4008", "5008", "Weekly Off Days Arrear");

        // --- 4009 Rest Days (legacy 2115-2122). Gate commented out at line 2118.
        tempNumber = await _dayCountService.GetRestDaysAsync(
            context.EmployeeId, context.PayDate, context.PayrollRunType, context.OffCycleEndDate);
        days = GetSalaryRegisterDaysList(context, context.PayStartDate, context.OffCycleEndDate, SalaryRegisterDayType.RestDay);
        await CalculateTimeTypeAsync(register, "4009", "Rest Days", tempNumber, days);

        // --- 5009 (legacy 2124-2130).
        await CalculateArrearAsync(context, regularOrEos, "4009", "5009", "Rest Days Arrear");

        // --- 4011 Active Days (legacy 2132-2138). Gate commented out at line 2135.
        tempNumber = await _dayCountService.GetActiveDaysAsync(
            context.EmployeeId, context.PayStartDate, context.PayDate, context.PayrollRunType, context.OffCycleEndDate);
        await CalculateTimeTypeAsync(register, "4011", "Active Days", tempNumber, null);

        // --- 5011 (legacy 2140-2146).
        await CalculateArrearAsync(context, regularOrEos, "4011", "5011", "Active Days Arrear");

        // --- 4020 Inactive Days (legacy 2148-2154). Gate commented out at line 2151.
        tempNumber = GetInactiveDays(context);
        await CalculateTimeTypeAsync(register, "4020", "Inactive Days", tempNumber, null);

        // --- 5020 (legacy 2156-2162).
        await CalculateArrearAsync(context, regularOrEos, "4020", "5020", "Inactive Days Arrear");

        // --- 4017 Lieu Paid Days (legacy 2180-2187). Gate commented out at line 2183.
        tempNumber = GetLieuPaidDays(context);
        days = GetSalaryRegisterDaysList(context, context.PayStartDate, context.OffCycleEndDate, SalaryRegisterDayType.LieuPaidDays);
        await CalculateTimeTypeAsync(register, "4017", "Lieu Paid Days", tempNumber, days);

        // --- 5017 (legacy 2189-2195).
        await CalculateArrearAsync(context, regularOrEos, "4017", "5017", "Lieu Paid Days Arrear");

        // --- 4014 Present Days (legacy 2197-2203). Gate commented out at line 2200. The legacy `ref
        //     salaryRegisterDaysList` out-parameter is PresentDaysResult.MonthWiseDays here.
        var presentDays = await _dayCountService.GetPresentDaysAsync(
            context.EmployeeId, context.PayStartDate, context.PayDate, register.RegisterId,
            context.PayrollRunType, context.OffCycleEndDate, context.OffCycleReason);
        await CalculateTimeTypeAsync(register, "4014", "Present Days", presentDays.PresentDays, presentDays.MonthWiseDays);

        // --- 5014 (legacy 2205-2211).
        await CalculateArrearAsync(context, regularOrEos, "4014", "5014", "Present Days Arrear");

        // --- 4015 Paid Days (legacy 2213-2219) = 4014 + 4002 + 4000 off THIS register, so it must run after
        //     all three have been written.
        tempNumber = regularOrEos ? await _dayCountService.GetPaidDaysAsync(register.RegisterId) : 0;
        await CalculateTimeTypeAsync(register, "4015", "Paid Days", tempNumber, null);

        // --- 5015 (legacy 2221-2227).
        await CalculateArrearAsync(context, regularOrEos, "4015", "5015", "Paid Days Arrear");

        // --- 4016 Extra Working Paid Days (legacy 2229-2239).
        tempNumber = 0;
        days = null;
        if (regularOrEos)
        {
            tempNumber = GetExtraWorkingPaidDays(context);
            days = GetSalaryRegisterDaysList(context, context.PayStartDate, context.OffCycleEndDate, SalaryRegisterDayType.AttendanceDay);
        }

        await CalculateTimeTypeAsync(register, "4016", "Extra Working Paid Days", tempNumber, days);

        // --- 5016 (legacy 2241-2247).
        await CalculateArrearAsync(context, regularOrEos, "4016", "5016", "Extra Working Paid Days Arrear");

        // --- 4021 Annual Leave Opening Balance (legacy 2249-2257).
        tempNumber = await AllowAdvancePayAsync(context, "4021", notEosOffCycle, isRegular)
            ? await GetAnnualLeaveOpeningBalanceAsync(context)
            : 0;
        await CalculateTimeTypeAsync(register, "4021", "Annual Leave Opening Balance", tempNumber, null);

        // --- 5021 (legacy 2259-2266). NOTE the DIFFERENT gate: "Regular || (OffCycle && reason != EOS)".
        await CalculateArrearAsync(context, isRegular || notEosOffCycle, "4021", "5021", "Annual Leave Opening Balance Arrear");

        // --- 4010 Annual Leave Current Month Accrual (legacy 2268-2276).
        tempNumber = await AllowAdvancePayAsync(context, "4010", notEosOffCycle, isRegular)
            ? GetAnnualLeaveCurrentMonthAccrual(context)
            : 0;
        await CalculateTimeTypeAsync(register, "4010", "Annual Leave Current Month Accrual", tempNumber, null);

        // --- 5010 (legacy 2278-2285).
        await CalculateArrearAsync(context, isRegular || notEosOffCycle, "4010", "5010", "Annual Leave Current Month Accrual Arrear");

        // --- 4030 Annual Leave Current Month Adjustment (legacy 2287-2295).
        tempNumber = await AllowAdvancePayAsync(context, "4030", notEosOffCycle, isRegular)
            ? GetAnnualLeaveCurrentMonthAdjustment(context)
            : 0;
        await CalculateTimeTypeAsync(register, "4030", "Annual Leave Current Month Adjustment", tempNumber, null);

        // --- 5030 (legacy 2297-2304).
        await CalculateArrearAsync(context, isRegular || notEosOffCycle, "4030", "5030", "Annual Leave Current Month Adjustment Arrear");

        // --- 4031 Annual Leave Forfeited Accrual (legacy 2306-2321). Whole block gated on the MONTHLY
        //     forfeit rule; when the rule is not Monthly legacy does not even call CalculateTimeType, so no
        //     line is written and any existing one keeps its old Number.
        if (context.Settings.ForfeitRule == ForfeitRule.Monthly)
        {
            tempNumber = await AllowAdvancePayAsync(context, "4031", notEosOffCycle, isRegular)
                ? GetAnnualLeaveForfeitedAccrual(context)
                : 0;
            await CalculateTimeTypeAsync(register, "4031", "Annual Leave Forfeited Accrual", tempNumber, null);
        }

        // --- 5031: legacy 2323-2330 is fully commented out. NOT PORTED.

        // --- 4022 Annual Leave Closing Balance (legacy 2332-2341). Reads 4021/4010/4000/4030/4031 and their
        //     5xxx twins back off this register, so it must run after all of them.
        tempNumber = await AllowAdvancePayAsync(context, "4022", notEosOffCycle, isRegular)
            ? await GetAnnualLeaveClosingBalanceAsync(context)
            : 0;
        await CalculateTimeTypeAsync(register, "4022", "Annual Leave Closing Balance", tempNumber, null);

        // --- 5022 (legacy 2343-2350).
        await CalculateArrearAsync(context, isRegular || notEosOffCycle, "4022", "5022", "Annual Leave Closing Balance Arrear");

        // --- 4023 Total Active Days (legacy 2352-2358). Reads 4011/5011 off this register.
        tempNumber = regularOrEos ? await GetTotalActiveDaysAsync(context) : 0;
        await CalculateTimeTypeAsync(register, "4023", "Total Active Days", tempNumber, null);

        // --- 5023 (legacy 2360-2366).
        await CalculateArrearAsync(context, regularOrEos, "4023", "5023", "Total Active Days Arrear");

        // --- 4024 Total Absent Unpaid Days (legacy 2368-2374). Reads 4001 off this register.
        tempNumber = regularOrEos ? await GetTotalAbsentUnpaidDaysAsync(context) : 0;
        await CalculateTimeTypeAsync(register, "4024", "Total Absent Unpaid Days", tempNumber, null);

        // --- 5024 (legacy 2376-2382).
        await CalculateArrearAsync(context, regularOrEos, "4024", "5024", "Total Absent Unpaid Days Arrear");

        // --- 4025 Total Indemnity Days (legacy 2384-2398).
        tempNumber = 0;
        if (await AllowAdvancePayAsync(context, "4025", notEosOffCycle, isRegular))
        {
            tempNumber = await GetTotalIndemnityDaysAsync(context, context.PayDate);
        }
        else if (isEos)
        {
            // Legacy 2390-2396 (the "17-KHUFF" case): on an End-of-Service off-cycle, recompute as of the
            // separation's LastDayInService rather than payDate.
            var separation = _employeeSeparationRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId && r.RevokeDate == null)
                .OrderByDescending(r => r.SubmissionDate)
                .FirstOrDefault();

            if (separation != null)
            {
                tempNumber = await GetTotalIndemnityDaysAsync(context, separation.LastDayInService);
            }
        }

        await CalculateTimeTypeAsync(register, "4025", "Total Indemnity Days", tempNumber, null);

        // --- 5025: legacy 2400-2407 is fully commented out. NOT PORTED.

        // --- 4004 Indemnity Days (legacy 2409-2416).
        tempNumber = await AllowAdvancePayAsync(context, "4004", notEosOffCycle, isRegular)
            ? await _indemnityService.GetIndemnityDaysAsync(
                context.EmployeeId, register.RegisterId, context.PayDate,
                GetEmployeeGroupId(context), GetEmployeeSubGroupId(context), null)
            : 0;
        await CalculateTimeTypeAsync(register, "4004", "Indemnity Days", tempNumber, null);

        // --- 5004 (legacy 2418-2425). NOTE this arrear reads wage type "5004"'s OWN IncludeInAdvancePay
        //     (legacy line 2421), not "4004"'s.
        tempNumber = await AllowAdvancePayAsync(context, "5004", notEosOffCycle, isRegular)
            ? await GetArrearAsync(context, "4004")
            : 0;
        await CalculateTimeTypeAsync(register, "5004", "Indemnity Days Arrear", tempNumber, null);

        // --- 4005 Indemnity Paid Days (legacy 2427-2434). UNGATED -- legacy line 2431's gate is commented
        //     out, and its `tempWageType = GetWageType("4005")` at line 2430 is then unused.
        tempNumber = GetIndemnityPaidDays(context);
        await CalculateTimeTypeAsync(register, "4005", "Indemnity Paid Days", tempNumber, null);

        // --- 5005: legacy 2436-2443 is fully commented out. NOT PORTED.

        // --- 4026 Total Indemnity Paid Days (legacy 2445-2452). Reads 4005/5005 off this register.
        tempNumber = await AllowAdvancePayAsync(context, "4026", notEosOffCycle, isRegular)
            ? await GetTotalIndemnityPaidDaysAsync(context)
            : 0;
        await CalculateTimeTypeAsync(register, "4026", "Total Indemnity Paid Days", tempNumber, null);

        // --- 5026: legacy 2454-2461 is fully commented out. NOT PORTED.

        // --- 4006 Indemnity Balance Days (legacy 2463-2470). Reads 4025/5025/4026 off this register.
        tempNumber = await AllowAdvancePayAsync(context, "4006", notEosOffCycle, isRegular)
            ? await GetIndemnityBalanceDaysAsync(context)
            : 0;
        await CalculateTimeTypeAsync(register, "4006", "Indemnity Balance Days", tempNumber, null);

        // --- 5006: legacy 2472-2479 is fully commented out. NOT PORTED.

        // --- 4027 Employee Ticket Current Month Accrual (legacy 2481-2487).
        tempNumber = regularOrEos ? GetEmployeeTicketAccrual(context) : 0;
        await CalculateTimeTypeAsync(register, "4027", "Employee Ticket Current Month Accrual", tempNumber, null);

        // --- 5027 (legacy 2489-2495).
        await CalculateArrearAsync(context, regularOrEos, "4027", "5027", "Employee Ticket Current Month Accrual Arrear");

        // --- 4028 Total Employee Ticket Accrual (legacy 2497-2503). Reads 4027/5027 off this register.
        tempNumber = regularOrEos ? await GetEmployeeTicketTotalAccrualAsync(context) : 0;
        await CalculateTimeTypeAsync(register, "4028", "Total Employee Ticket Accrual", tempNumber, null);

        // --- 5028 (legacy 2505-2511).
        await CalculateArrearAsync(context, regularOrEos, "4028", "5028", "Total Employee Ticket Accrual Arrear");

        // --- Update Indemnity Quota (legacy 2513-2516).
        if (regularOrEos)
        {
            await _indemnityService.UpdateEmployeeIndemnityQuotaAsync(context.EmployeeId, register.RegisterId);
        }
    }

    #endregion

    #region << CalculateTimeType writer (legacy Program.cs line 2717) >>

    /// <summary>
    /// Legacy <c>Program.CalculateTimeType</c> (lines 2717-2747), the ONE writer every branch above funnels
    /// through.
    ///
    /// <para>Faithfulness notes:</para>
    /// <list type="bullet">
    /// <item><description><b><c>tempNumber == 0</c> writes NOTHING</b> (legacy line 2719) -- it does not
    /// create a line AND it does not clear an existing one. This is why
    /// <see cref="IPayrollRegisterStore.SaveLineAsync"/> is called with
    /// <c>deleteWhenAmountIsZero: false</c>: these lines carry Number only, and Amount is legitimately 0 for
    /// all of them, so the store's "zero amount means delete" rule must be switched off or every day-count row
    /// would be deleted the instant it was written.</description></item>
    /// <item><description><b>Rate and Amount are PRESERVED, not zeroed</b> -- legacy explicitly comments out
    /// <c>salaryRegisterDetail.Rate = 0; salaryRegisterDetail.Amount = 0;</c> (lines 2730-2731) and assigns
    /// only <c>Number</c>. The existing line's Rate/Amount are therefore read back and re-sent.</description></item>
    /// <item><description><b><c>IsSystemGenerated = true</c></b> (legacy line 2733) -- the column now exists
    /// on <c>SalaryRegisterDetail</c> (migration <c>20260729111551</c>), and the store stamps it on every
    /// save.</description></item>
    /// <item><description><b>Missing wage type throws</b> with legacy's exact message shape
    /// <c>"WageType {code} : {message} not found"</c> (line 2728). Legacy's check is unreachable dead code
    /// (it dereferences <c>salaryRegisterDetail.WageType</c> only when the row was newly created, but XPO's
    /// <c>CreateObject</c> path is the only one that could be null); the check is nevertheless ported to the
    /// point where it CAN fire, i.e. before the write.</description></item>
    /// <item><description><b>Day rows</b> (legacy 2742-2745 -&gt; <c>AddSalaryRegisterDaysDetail</c>, line
    /// 5439) skip any row whose Rate, Number AND Amount are all 0 (line 5443). An EMPTY/absent list means
    /// legacy did nothing at all, so <see cref="IPayrollRegisterStore.SaveDayDetailsAsync"/> -- which
    /// REPLACES the breakdown -- is not called, and pre-existing day rows survive.</description></item>
    /// </list>
    /// </summary>
    protected virtual async Task CalculateTimeTypeAsync(
        PayrollRegisterRef register,
        string wageCode,
        string message,
        double tempNumber,
        List<SalaryRegisterDayCount> salaryRegisterDaysList)
    {
        // Legacy line 2719.
        if (tempNumber == 0)
        {
            return;
        }

        var wageType = _wageTypeRepository.GetAll().FirstOrDefault(r => r.Code == wageCode);
        if (wageType == null)
        {
            // Legacy line 2728, verbatim message shape.
            throw new UserFriendlyException("WageType " + wageCode + " : " + message + " not found");
        }

        var existing = await _registerStore.GetLineAsync(register, wageCode);

        var line = new PayrollRegisterLine
        {
            Id = existing?.Id ?? 0,
            WageTypeId = wageType.Id,
            WageCode = wageCode,
            // Legacy lines 2730-2731 are commented out: Rate/Amount are left exactly as they were.
            Rate = existing?.Rate ?? 0,
            Amount = existing?.Amount ?? 0,
            Number = tempNumber,
            IsSystemGenerated = true
        };

        var saved = await _registerStore.SaveLineAsync(register, line, deleteWhenAmountIsZero: false);

        if (saved == null || salaryRegisterDaysList == null)
        {
            return;
        }

        // Legacy AddSalaryRegisterDaysDetail line 5443: all-zero rows are not written.
        var rows = salaryRegisterDaysList
            .Where(d => d.Rate != 0 || d.Number != 0 || d.Amount != 0)
            .ToList();

        if (rows.Count == 0)
        {
            return;
        }

        await _registerStore.SaveDayDetailsAsync(register, saved.Id, rows);
    }

    #endregion

    #region << GetArrear (legacy Program.cs line 5302) >>

    /// <summary>
    /// Wraps <see cref="GetArrearAsync"/> in the legacy call-site shape shared by all 22 live 5xxx blocks:
    /// evaluate to 0 when the gate is false, then always call the writer (which itself no-ops on 0).
    /// </summary>
    private async Task CalculateArrearAsync(
        PayrollCalculationContext context,
        bool gate,
        string sourceWageCode,
        string arrearWageCode,
        string message)
    {
        var tempNumber = gate ? await GetArrearAsync(context, sourceWageCode) : 0;
        await CalculateTimeTypeAsync(context.Register, arrearWageCode, message, tempNumber, null);
    }

    /// <summary>
    /// Legacy <c>Program.GetArrear</c> (lines 5302-5342). For each of THIS run's other retro registers
    /// (<c>currentSalRegisters</c>): add that register's figure for <paramref name="wageTypeCode"/>, then
    /// subtract whatever was already paid for the same <c>ForPeriod</c> on the latest earlier-paid REGULAR
    /// register. <c>BillingType.TimeType</c> wage types use <c>Number</c>; everything else uses
    /// <c>Amount</c>.
    ///
    /// <para>Preserved verbatim: the whole calculation is skipped unless
    /// <c>payDate == payInDate</c> (i.e. only the CURRENT period's register carries the arrear roll-up), and
    /// a register missing the line contributes 0 rather than being skipped -- legacy line 5318's comment
    /// explains that "continue" was deliberately removed so deleted annual leaves still produce
    /// arrears.</para>
    ///
    /// <para>Legacy's <c>empSalaryRegister</c> static (all of the employee's registers) becomes a repository
    /// query filtered by <c>EmployeeId</c>. Legacy's <c>r.InPeriod &lt; payInDate</c> and
    /// <c>PayrollRunType.Regular</c> filters are kept exactly (lines 5325-5326).</para>
    /// </summary>
    protected virtual async Task<double> GetArrearAsync(PayrollCalculationContext context, string wageTypeCode)
    {
        double arrear = 0;

        // Legacy line 5307.
        if (context.PayDate != context.PayInDate || context.CurrentSalaryRegisters.Count == 0)
        {
            return arrear;
        }

        var wageType = _wageTypeRepository.GetAll()
            .Where(r => r.Code == wageTypeCode)
            .Select(r => new { r.Id, r.BillingType })
            .FirstOrDefault();

        if (wageType == null)
        {
            // Legacy line 5311.
            throw new UserFriendlyException(string.Format("WageType {0} not found", wageTypeCode));
        }

        var isTimeType = wageType.BillingType == BillingType.TimeType;

        foreach (var currentSalReg in context.CurrentSalaryRegisters)
        {
            var currentLine = await _registerStore.GetLineByWageTypeIdAsync(ToRegisterRef(currentSalReg), wageType.Id);
            if (currentLine != null)
            {
                arrear = arrear + (isTimeType ? currentLine.Number : currentLine.Amount);
            }

            // Legacy lines 5322-5327.
            var lastSalReg = _salaryRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.ForPeriod == currentSalReg.ForPeriod
                            && r.InPeriod < context.PayInDate
                            && r.PayrollRunType == PayrollRunType.Regular)
                .OrderByDescending(r => r.InPeriod)
                .FirstOrDefault();

            if (lastSalReg == null)
            {
                continue;
            }

            var lastLine = await _registerStore.GetLineByWageTypeIdAsync(ToRegisterRef(lastSalReg), wageType.Id);
            if (lastLine != null)
            {
                arrear = arrear - (isTimeType ? lastLine.Number : lastLine.Amount);
            }
        }

        return arrear;
    }

    #endregion

    #region << GetSalaryRegisterDaysList (legacy Program.cs line 5387) >>

    /// <summary>
    /// Legacy <c>Program.GetSalaryRegisterDaysList</c> (lines 5387-5438): walks
    /// <paramref name="startDate"/>..<paramref name="endDate"/> one CALENDAR MONTH at a time and emits one
    /// breakdown row per month, stamped with that month's LAST day as its <c>Period</c>.
    ///
    /// <para>Runs only on OFF-CYCLE runs -- legacy's other condition,
    /// <c>Program.calculateSalRegDaysDetail</c>, is a hardcoded <c>true</c> literal (legacy line 131, never
    /// re-assigned).</para>
    ///
    /// <para><b>Legacy's empty catch is preserved</b> (lines 5433-5436): any failure returns the
    /// partially-built list rather than propagating. That is observable behaviour, not error handling, so
    /// removing it would change results.</para>
    /// </summary>
    protected virtual List<SalaryRegisterDayCount> GetSalaryRegisterDaysList(
        PayrollCalculationContext context,
        DateTime startDate,
        DateTime endDate,
        SalaryRegisterDayType dayType)
    {
        var salaryRegisterDaysList = new List<SalaryRegisterDayCount>();

        try
        {
            if (context.PayrollRunType != PayrollRunType.OffCycle)
            {
                return salaryRegisterDaysList;
            }

            var approvalOn = context.Settings.EnableTimeRegisterApproval;
            var lieuLeaveTypeId = GetAbsenceTypeIdByCode("9");

            while (startDate <= endDate)
            {
                var payStartDate = startDate;
                var payEndDate = MonthEnd(payStartDate);
                if (payEndDate > endDate)
                {
                    payEndDate = endDate;
                }

                var rows = context.TimeRegisterQuery
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.DayDate >= payStartDate
                                && r.DayDate <= payEndDate)
                    .ToList();

                // Legacy lines 5405-5414, arm for arm.
                double days;
                switch (dayType)
                {
                    case SalaryRegisterDayType.PublicHoliday:
                        days = rows.Sum(r => r.PublicHoliday);
                        break;
                    case SalaryRegisterDayType.WeeklyOff:
                        days = rows.Sum(r => r.WeeklyOff);
                        break;
                    case SalaryRegisterDayType.RestDay:
                        days = rows.Where(r => approvalOn ? r.ApprovedAbsentDay == 0 : r.AbsentDay == 0).Sum(r => r.RestDay);
                        break;
                    case SalaryRegisterDayType.SickLeavePaidDay:
                        days = rows.Sum(r => r.SickLeavePaidDay);
                        break;
                    case SalaryRegisterDayType.SickLeaveUnpaidDay:
                        days = rows.Sum(r => r.SickLeaveUnpaidDay);
                        break;
                    case SalaryRegisterDayType.AnnualLeaveDay:
                        days = rows.Sum(r => r.AnnualLeaveDay + r.LeaveAdjDay);
                        break;
                    case SalaryRegisterDayType.AttendanceDay:
                        days = rows.Where(r => r.AttendanceTypeId != null).Sum(r => r.AttendanceDay);
                        break;
                    case SalaryRegisterDayType.LieuPaidDays:
                        days = lieuLeaveTypeId == null
                            ? 0
                            : rows.Where(r => r.LeaveTypeId == lieuLeaveTypeId).Sum(r => r.LeaveDay);
                        break;
                    case SalaryRegisterDayType.AbsentDay:
                        days = rows.Where(r => r.UnpaidLeaveDay == 0)
                            .Sum(r => approvalOn ? r.ApprovedAbsentDay : r.AbsentDay);
                        break;
                    default:
                        days = 0;
                        break;
                }

                salaryRegisterDaysList.Add(new SalaryRegisterDayCount
                {
                    Period = MonthEnd(payStartDate),
                    Rate = 0,
                    Number = days,
                    Amount = 0
                });

                startDate = payEndDate.AddDays(1);
            }
        }
        catch (Exception)
        {
            // Legacy lines 5433-5436: swallowed on purpose -- see method remarks.
        }

        return salaryRegisterDaysList;
    }

    #endregion

    #region << 4000 / 4010 / 4021 / 4022 / 4030 / 4031 -- Annual leave family >>

    /// <summary>
    /// Legacy <c>Program.GetAnnualLeaveCurrentMonthPaidDays</c> (lines 3247-3282). Wage code <b>4000</b>.
    ///
    /// <para>Regular: sum <c>AnnualLeaveDay + LeaveAdjDay</c> over the pay window. Off-cycle: sum the same
    /// columns, but only over the date ranges of the employee's PayInAdvance annual-leave requests whose
    /// <c>PaymentDate</c> equals <c>offCyclePaymentDate</c>, floored at <c>payStartDate</c> (legacy line
    /// 3270's TGR fix).</para>
    ///
    /// <para>Legacy's <c>LeaveAdjDay</c> is nullable and coalesced to 0; this port's column is a non-nullable
    /// <c>double</c>, so the coalesce is a no-op and is dropped.</para>
    /// </summary>
    private double GetAnnualLeaveCurrentMonthPaidDays(PayrollCalculationContext context)
    {
        double daysCount = 0;

        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            // Legacy reads empTimeRegisterList, which is never null on a Regular run (legacy line 1614).
            var rows = context.TimeRegisterList;
            if (rows == null)
            {
                return 0;
            }

            return rows
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= context.PayStartDate
                            && r.DayDate <= context.PayDate)
                .Sum(r => r.AnnualLeaveDay + r.LeaveAdjDay);
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            var annualLeaveTypeId = context.Settings.AnnualLeaveAbsenceTypeId;

            var absences = _employeeAbsenceRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && (r.AbsenceTypeId == annualLeaveTypeId || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                            && r.LeavePaymentType == LeavePaymentType.PayInAdvance
                            && r.PaymentDate == context.OffCyclePaymentDate)
                .Select(r => new { r.StartDate, r.EndDate })
                .ToList();

            foreach (var rec in absences)
            {
                var recStart = rec.StartDate;
                var recEnd = rec.EndDate;

                daysCount = daysCount + context.TimeRegisterQuery
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.DayDate >= context.PayStartDate
                                && r.DayDate >= recStart
                                && r.DayDate <= recEnd)
                    .Sum(r => r.AnnualLeaveDay + r.LeaveAdjDay);
            }
        }

        return daysCount;
    }

    /// <summary>
    /// Legacy <c>Program.GetAnnualLeaveCurrentMonthAccrual</c> (lines 3189-3217). Wage code <b>4010</b>.
    /// Sums the <c>TimeRegister.AnnualLeaveAccrualPerDay</c> column the time engine already wrote.
    ///
    /// <para>Off-cycle ASSIGNS <c>payEndDate = offCycleEndDate</c> (it can EXTEND the window), matching
    /// legacy line 3206 -- not the min() shortening form used elsewhere. Ported verbatim.</para>
    /// </summary>
    private double GetAnnualLeaveCurrentMonthAccrual(PayrollCalculationContext context)
    {
        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            var rows = context.TimeRegisterList;
            if (rows == null)
            {
                return 0;
            }

            return rows
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= context.PayStartDate
                            && r.DayDate <= context.PayDate)
                .Sum(r => r.AnnualLeaveAccrualPerDay);
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            var payEndDate = context.OffCycleEndDate;

            return context.TimeRegisterQuery
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= context.PayStartDate
                            && r.DayDate <= payEndDate)
                .Sum(r => r.AnnualLeaveAccrualPerDay);
        }

        return 0;
    }

    /// <summary>
    /// Legacy <c>Program.GetAnnualLeaveCurrentMonthAdjustment</c> (lines 3218-3246). Wage code <b>4030</b>.
    /// Sums <c>EmployeeAbsAdjustment.QuotaAdjusted</c> for annual leave.
    ///
    /// <para>Legacy's two branches are textually IDENTICAL apart from the off-cycle one assigning
    /// <c>payEndDate = offCycleEndDate</c> first; both are ported.</para>
    /// </summary>
    private double GetAnnualLeaveCurrentMonthAdjustment(PayrollCalculationContext context)
    {
        var payEndDate = context.PayrollRunType == PayrollRunType.OffCycle
            ? context.OffCycleEndDate
            : context.PayDate;

        var annualLeaveTypeId = context.Settings.AnnualLeaveAbsenceTypeId;

        return _employeeAbsAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && (r.AbsenceTypeId == annualLeaveTypeId || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                        && r.EffectiveDate >= context.PayStartDate
                        && r.EffectiveDate <= payEndDate)
            .Sum(r => r.QuotaAdjusted);
    }

    /// <summary>
    /// Legacy <c>Program.GetAnnualLeaveForfeitedAccrual</c> (lines 3283-3311). Wage code <b>4031</b>.
    ///
    /// <para><b>Note the legacy asymmetry, preserved:</b> unlike 4030, this one matches ONLY
    /// <c>AbsenceType.Oid == AnnualLeaveOid</c> -- it does NOT have the
    /// <c>|| QuotaType == QuotaType.AnnualLeave</c> second arm (legacy lines 3294 and 3301). Adding it would
    /// forfeit more days than legacy.</para>
    /// </summary>
    private double GetAnnualLeaveForfeitedAccrual(PayrollCalculationContext context)
    {
        var payEndDate = context.PayrollRunType == PayrollRunType.OffCycle
            ? context.OffCycleEndDate
            : context.PayDate;

        var annualLeaveTypeId = context.Settings.AnnualLeaveAbsenceTypeId;

        return _employeeAbsQuotaForfeitRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.AbsenceTypeId == annualLeaveTypeId
                        && r.EffectiveDate >= context.PayStartDate
                        && r.EffectiveDate <= payEndDate)
            .Sum(r => r.QuotaForfeited);
    }

    /// <summary>
    /// Legacy <c>Program.GetAnnualLeaveOpeningBalance</c> (lines 3125-3188). Wage code <b>4021</b>.
    ///
    /// <para>Three-step fallback: (1) the previous month's REGULAR non-retro register's 4022; else (2) the
    /// latest LATER-PAID regular register for the same previous <c>ForPeriod</c>'s 4022; else (3) a full
    /// from-scratch recomputation over all history up to the previous month end
    /// (<c>accrual - (used + adjusted + forfeited)</c>).</para>
    ///
    /// <para>Legacy's <c>prevSalRegList</c> is the engine's per-employee "registers whose ForPeriod is the
    /// previous month end AND whose run type matches this run" list, so both lookups here run over
    /// <see cref="PayrollCalculationContext.PreviousSalaryRegisters"/>.</para>
    /// </summary>
    private async Task<double> GetAnnualLeaveOpeningBalanceAsync(PayrollCalculationContext context)
    {
        double daysCount = 0;

        var prevMonthEndDate = MonthEnd(context.PayDate.AddMonths(-1));

        // Legacy lines 3137-3141.
        var prevSalReg = context.PreviousSalaryRegisters
            .FirstOrDefault(r => r.ForPeriod == prevMonthEndDate
                                 && r.InPeriod == prevMonthEndDate
                                 && r.PayrollRunType == PayrollRunType.Regular);

        if (prevSalReg != null)
        {
            return daysCount + await GetLineNumberAsync(prevSalReg, "4022");
        }

        // Legacy lines 3150-3154.
        var prevReg = context.PreviousSalaryRegisters
            .Where(r => r.ForPeriod == prevMonthEndDate
                        && r.InPeriod > prevMonthEndDate
                        && r.PayrollRunType == PayrollRunType.Regular)
            .OrderByDescending(r => r.InPeriod)
            .FirstOrDefault();

        if (prevReg != null)
        {
            return daysCount + await GetLineNumberAsync(prevReg, "4022");
        }

        // Legacy lines 3160-3179: no register at all -- recompute from history.
        var annualLeaveTypeId = context.Settings.AnnualLeaveAbsenceTypeId;

        var totalAccrual = context.TimeRegisterQuery
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate <= prevMonthEndDate)
            .Sum(r => r.AnnualLeaveAccrualPerDay);

        var totalUsed = context.TimeRegisterQuery
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate <= prevMonthEndDate)
            .Sum(r => r.AnnualLeaveDay + r.LeaveAdjDay);

        var totalAdjusted = _employeeAbsAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && (r.AbsenceTypeId == annualLeaveTypeId || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                        && r.EffectiveDate <= prevMonthEndDate)
            .Sum(r => r.QuotaAdjusted);

        var totalForfeit = _employeeAbsQuotaForfeitRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId && r.EffectiveDate <= prevMonthEndDate)
            .Sum(r => r.QuotaForfeited);

        return totalAccrual - (totalUsed + totalAdjusted + totalForfeit);
    }

    /// <summary>
    /// Legacy <c>Program.GetAnnualLeaveClosingBalance</c> (lines 3312-3369). Wage code <b>4022</b>. A pure
    /// roll-up of THIS register's own lines:
    /// <c>4021 + 4010 - [4000] - 5000 - [4030] - 5030 - [4031] - 5031</c>, where the bracketed 4xxx terms are
    /// subtracted ONLY when <c>ForPeriod == InPeriod</c> (i.e. this is the current period, not a retro one)
    /// -- legacy lines 3337, 3347, 3357. The 5xxx arrear terms are subtracted unconditionally.
    ///
    /// <para>Legacy also has 5021 and 5010 additions commented out (lines 3323, 3330) after a documented
    /// back-and-forth between the Mashaan and TGR client cases; they stay commented out here.</para>
    /// </summary>
    private async Task<double> GetAnnualLeaveClosingBalanceAsync(PayrollCalculationContext context)
    {
        var register = context.Register;
        var isCurrentPeriod = register.ForPeriod == register.InPeriod;

        double daysCount = 0;

        daysCount = daysCount + await GetLineNumberAsync(register, "4021");
        daysCount = daysCount + await GetLineNumberAsync(register, "4010");

        if (isCurrentPeriod)
        {
            daysCount = daysCount - await GetLineNumberAsync(register, "4000");
        }

        daysCount = daysCount - await GetLineNumberAsync(register, "5000");

        if (isCurrentPeriod)
        {
            daysCount = daysCount - await GetLineNumberAsync(register, "4030");
        }

        daysCount = daysCount - await GetLineNumberAsync(register, "5030");

        if (isCurrentPeriod)
        {
            daysCount = daysCount - await GetLineNumberAsync(register, "4031");
        }

        daysCount = daysCount - await GetLineNumberAsync(register, "5031");

        return daysCount;
    }

    #endregion

    #region << 4001 / 4024 -- Absent unpaid family >>

    /// <summary>
    /// Legacy <c>Program.GetAbsentUnpaidDays</c> (lines 4406-4460). Wage code <b>4001</b>.
    ///
    /// <para><b>This is NOT the same calculation as 4029</b> (<see cref="IPayrollDayCountService.GetAbsentDaysAsync"/>,
    /// legacy <c>GetAbsentDays</c> line 5042), despite the similar name. 4001 is the sum of FOUR things,
    /// each gated on its OWN wage type's <c>IncludeInIndemnity</c> flag:</para>
    /// <list type="number">
    /// <item><description>gated on <b>4001</b>.<c>IncludeInIndemnity</c>: <c>TimeRegister.UnpaidLeaveDay</c>,
    /// plus <c>LeaveDay</c> on rows whose leave type is AbsenceType code "15" (Unapproved Absence Leave, used
    /// when the employee is on a Planned time policy), plus unpaid-leave
    /// <c>EmployeeAbsAdjustment.QuotaAdjusted</c>;</description></item>
    /// <item><description>gated on <b>4029</b>.<c>IncludeInIndemnity</c>: <c>AbsentDay</c> (or
    /// <c>ApprovedAbsentDay</c> under the approval workflow) on rows carrying NO unpaid-leave
    /// day.</description></item>
    /// </list>
    ///
    /// <para>The window comes from the shared cut-off shift (legacy <c>GetCutoffSetting</c>, invoked at line
    /// 4413 with wage code "4001"), then -- legacy line 4416, the 2025 ASF fix -- an off-cycle run on a
    /// NON-retro register clamps <c>payEndDate</c> to <c>offCycleEndDate</c>.</para>
    ///
    /// <para><b>Cut-off reuse.</b> <c>GetCutoffSetting</c> is already ported as the private
    /// <c>PayrollDayCountService.ApplyCutoffSetting</c>. It is NOT public, and this service must not take a
    /// dependency on another service's private helper, so the same shift is recomputed here from the same two
    /// inputs (<c>WageType.IncludeInCutoff</c> and <c>PayrollArea.CutOffDay</c>). If that rule ever changes,
    /// change both -- the situation is identical to the deliberately-duplicated
    /// <c>Allow_CalcWageType</c> documented on
    /// <see cref="PayrollEngineService"/>.</para>
    /// </summary>
    private async Task<double> GetAbsentUnpaidDaysAsync(PayrollCalculationContext context)
    {
        double absentCount = 0;

        var (payStartDate, payEndDate) = ApplyCutoffSetting(
            "4001", context.PayStartDate, context.PayDate, context.PayrollArea);

        // Legacy line 4416 (ASF / Emp 200089 / August-2025).
        if (context.PayrollRunType == PayrollRunType.OffCycle
            && context.Register.InPeriod == context.Register.ForPeriod)
        {
            payEndDate = context.OffCycleEndDate;
        }

        var approvalOn = context.Settings.EnableTimeRegisterApproval;

        // Legacy lines 4419-4440.
        if (HasWageTypeIncludedInIndemnity("4001"))
        {
            var unapprovedAbsenceLeaveTypeId = GetAbsenceTypeIdByCode("15");
            var unpaidLeaveTypeId = GetAbsenceTypeIdByCode("2");

            absentCount = absentCount + context.TimeRegisterQuery
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= payStartDate
                            && r.DayDate <= payEndDate)
                .Sum(r => r.UnpaidLeaveDay);

            if (unapprovedAbsenceLeaveTypeId != null)
            {
                absentCount = absentCount + context.TimeRegisterQuery
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.DayDate >= payStartDate
                                && r.DayDate <= payEndDate
                                && r.LeaveTypeId == unapprovedAbsenceLeaveTypeId)
                    .Sum(r => r.LeaveDay);
            }

            if (unpaidLeaveTypeId != null)
            {
                absentCount = absentCount + _employeeAbsAdjustmentRepository.GetAll()
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.EffectiveDate >= payStartDate
                                && r.EffectiveDate <= payEndDate
                                && r.AbsenceTypeId == unpaidLeaveTypeId)
                    .Sum(r => r.QuotaAdjusted);
            }
        }

        // Legacy lines 4442-4452.
        if (HasWageTypeIncludedInIndemnity("4029"))
        {
            absentCount = absentCount + context.TimeRegisterQuery
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= payStartDate
                            && r.DayDate <= payEndDate
                            && r.UnpaidLeaveDay == 0)
                .Sum(r => approvalOn ? r.ApprovedAbsentDay : r.AbsentDay);
        }

        await Task.CompletedTask;
        return absentCount;
    }

    /// <summary>
    /// Legacy <c>Program.GetTotalAbsentUnpaidDays</c> (lines 4461-4551). Wage code <b>4024</b>: the running
    /// TOTAL of unpaid/absent days.
    ///
    /// <para>Structure: opening balance from <c>TimeTransfer</c> rows of transfer type code "5"
    /// ("Absent Unpaid Days Opening Balance", the 2019 IHPG fix) in the current month, PLUS the previous
    /// month's register's 4024, PLUS this register's own 4001. When there is no previous register, the
    /// previous-month total is recomputed from scratch over ALL history up to the previous month end using
    /// the same four components as 4001 (note legacy's <c>recStartDate</c> lower bounds are commented out at
    /// lines 4499/4510/4513/4519/4530, so those sums genuinely run from the beginning of time -- preserved).</para>
    ///
    /// <para>The 5024 addition is commented out in legacy (line 4542, Mashaan case E035) and stays out.</para>
    /// </summary>
    private async Task<double> GetTotalAbsentUnpaidDaysAsync(PayrollCalculationContext context)
    {
        double daysCount = 0;

        var payStartDate = new DateTime(context.PayDate.Year, context.PayDate.Month, 1, 0, 0, 0);
        var payEndDate = context.PayDate;
        var prevMonthEndDate = MonthEnd(context.PayDate.AddMonths(-1));

        var approvalOn = context.Settings.EnableTimeRegisterApproval;

        // Legacy lines 4470-4477.
        daysCount = daysCount + _timeTransferRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.TransferType.Code == "5"
                        && r.TransferDate >= payStartDate
                        && r.TransferDate <= payEndDate)
            .Sum(r => r.TransferCount);

        // Legacy lines 4479-4488. Legacy orders by Oid descending; Id is this port's Oid.
        var prevSalReg = context.PreviousSalaryRegisters
            .Where(r => r.ForPeriod == prevMonthEndDate)
            .OrderByDescending(r => r.Id)
            .FirstOrDefault();

        if (prevSalReg != null)
        {
            daysCount = daysCount + await GetLineNumberAsync(prevSalReg, "4024");
        }
        else
        {
            // Legacy lines 4493-4536: recompute the whole opening total.
            daysCount = daysCount + _timeTransferRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.TransferType.Code == "5"
                            && r.TransferDate <= prevMonthEndDate)
                .Sum(r => r.TransferCount);

            if (HasWageTypeIncludedInIndemnity("4001"))
            {
                var unapprovedAbsenceLeaveTypeId = GetAbsenceTypeIdByCode("15");
                var unpaidLeaveTypeId = GetAbsenceTypeIdByCode("2");

                daysCount = daysCount + context.TimeRegisterQuery
                    .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate <= prevMonthEndDate)
                    .Sum(r => r.UnpaidLeaveDay);

                if (unapprovedAbsenceLeaveTypeId != null)
                {
                    daysCount = daysCount + context.TimeRegisterQuery
                        .Where(r => r.EmployeeId == context.EmployeeId
                                    && r.DayDate <= prevMonthEndDate
                                    && r.LeaveTypeId == unapprovedAbsenceLeaveTypeId)
                        .Sum(r => r.LeaveDay);
                }

                if (unpaidLeaveTypeId != null)
                {
                    daysCount = daysCount + _employeeAbsAdjustmentRepository.GetAll()
                        .Where(r => r.EmployeeId == context.EmployeeId
                                    && r.EffectiveDate <= prevMonthEndDate
                                    && r.AbsenceTypeId == unpaidLeaveTypeId)
                        .Sum(r => r.QuotaAdjusted);
                }
            }

            if (HasWageTypeIncludedInIndemnity("4029"))
            {
                daysCount = daysCount + context.TimeRegisterQuery
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.DayDate <= prevMonthEndDate
                                && r.UnpaidLeaveDay == 0)
                    .Sum(r => approvalOn ? r.ApprovedAbsentDay : r.AbsentDay);
            }
        }

        // Legacy lines 4540-4543.
        daysCount = daysCount + await GetLineNumberAsync(context.Register, "4001");

        return daysCount;
    }

    #endregion

    #region << 4016 / 4017 / 4020 / 4023 -- TimeRegister column sums and roll-ups >>

    /// <summary>
    /// Legacy <c>Program.GetInactiveDays</c> (lines 4952-4980). Wage code <b>4020</b>. Sums
    /// <c>TimeRegister.InactiveDay</c>.
    ///
    /// <para>Off-cycle uses the <c>min(payEndDate, offCycleEndDate)</c> SHORTENING form (legacy line 4966),
    /// unlike 4010/4017 which assign and can extend. Ported verbatim -- the inconsistency is legacy's.</para>
    /// </summary>
    private double GetInactiveDays(PayrollCalculationContext context)
    {
        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            var rows = context.TimeRegisterList;
            if (rows == null)
            {
                return 0;
            }

            return rows
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= context.PayStartDate
                            && r.DayDate <= context.PayDate)
                .Sum(r => r.InactiveDay);
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            var payEndDate = context.PayDate < context.OffCycleEndDate ? context.PayDate : context.OffCycleEndDate;

            return context.TimeRegisterQuery
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= context.PayStartDate
                            && r.DayDate <= payEndDate)
                .Sum(r => r.InactiveDay);
        }

        return 0;
    }

    /// <summary>
    /// Legacy <c>Program.GetExtraWorkingPaidDays</c> (lines 4981-5009). Wage code <b>4016</b>. Sums
    /// <c>TimeRegister.AttendanceDay</c> on rows that carry an attendance type.
    ///
    /// <para><b>Legacy quirk preserved:</b> the off-cycle branch does NOT narrow <c>payEndDate</c> at all --
    /// it uses the same window as Regular and simply reads the unfiltered query instead of the list (legacy
    /// lines 4993-5000). <c>offCycleEndDate</c> is accepted and ignored.</para>
    /// </summary>
    private double GetExtraWorkingPaidDays(PayrollCalculationContext context)
    {
        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            var rows = context.TimeRegisterList;
            if (rows == null)
            {
                return 0;
            }

            return rows
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= context.PayStartDate
                            && r.DayDate <= context.PayDate
                            && r.AttendanceTypeId != null)
                .Sum(r => r.AttendanceDay);
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            return context.TimeRegisterQuery
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= context.PayStartDate
                            && r.DayDate <= context.PayDate
                            && r.AttendanceTypeId != null)
                .Sum(r => r.AttendanceDay);
        }

        return 0;
    }

    /// <summary>
    /// Legacy <c>Program.GetLieuPaidDays</c> (lines 5010-5041). Wage code <b>4017</b>. Sums
    /// <c>TimeRegister.LeaveDay</c> on rows whose leave type is AbsenceType code "9" (Lieu).
    ///
    /// <para>Off-cycle ASSIGNS <c>payEndDate = offCycleEndDate</c> (legacy line 5026) -- the extending
    /// form.</para>
    /// </summary>
    private double GetLieuPaidDays(PayrollCalculationContext context)
    {
        var lieuLeaveTypeId = GetAbsenceTypeIdByCode("9");
        if (lieuLeaveTypeId == null)
        {
            // Legacy's `r.LeaveType != null && r.LeaveType.Code == "9"` matches nothing when no such
            // AbsenceType exists; same result.
            return 0;
        }

        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            var rows = context.TimeRegisterList;
            if (rows == null)
            {
                return 0;
            }

            return rows
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= context.PayStartDate
                            && r.DayDate <= context.PayDate
                            && r.LeaveTypeId == lieuLeaveTypeId)
                .Sum(r => r.LeaveDay);
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            var payEndDate = context.OffCycleEndDate;

            return context.TimeRegisterQuery
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= context.PayStartDate
                            && r.DayDate <= payEndDate
                            && r.LeaveTypeId == lieuLeaveTypeId)
                .Sum(r => r.LeaveDay);
        }

        return 0;
    }

    /// <summary>
    /// Legacy <c>Program.GetTotalActiveDays</c> (lines 4923-4951). Wage code <b>4023</b>: previous month's
    /// register's <c>4023 + 5023</c>, plus this register's <c>4011 + 5011</c>. Touches no TimeRegister data.
    /// </summary>
    private async Task<double> GetTotalActiveDaysAsync(PayrollCalculationContext context)
    {
        double daysCount = 0;

        var prevMonthEndDate = MonthEnd(context.PayDate.AddMonths(-1));

        var prevSalReg = context.PreviousSalaryRegisters
            .Where(r => r.ForPeriod == prevMonthEndDate)
            .OrderByDescending(r => r.Id)
            .FirstOrDefault();

        if (prevSalReg != null)
        {
            daysCount = daysCount + await GetLineNumberAsync(prevSalReg, "4023");
            daysCount = daysCount + await GetLineNumberAsync(prevSalReg, "5023");
        }

        daysCount = daysCount + await GetLineNumberAsync(context.Register, "4011");
        daysCount = daysCount + await GetLineNumberAsync(context.Register, "5011");

        return daysCount;
    }

    #endregion

    #region << 4005 / 4006 / 4025 / 4026 -- Indemnity day family >>

    /// <summary>
    /// Legacy <c>Program.GetTotalIndemnityDays</c> (line 3391) -- <b>delegated</b> to
    /// <see cref="IAccrualService.GetTotalIndemnityDaysAsync"/>, which is the already-ported copy of that
    /// exact algorithm (see <see cref="IIndemnityCalculationService"/>'s remarks, which record the same
    /// mapping). Wage code <b>4025</b>.
    ///
    /// <para><paramref name="asOnDate"/> is <c>payDate</c> on the normal path and the separation's
    /// <c>LastDayInService</c> on the End-of-Service off-cycle path (legacy line 2395).</para>
    /// </summary>
    private Task<double> GetTotalIndemnityDaysAsync(PayrollCalculationContext context, DateTime asOnDate)
    {
        return _accrualService.GetTotalIndemnityDaysAsync(
            context.EmployeeId,
            asOnDate,
            GetEmployeeGroupId(context),
            GetEmployeeSubGroupId(context),
            // Legacy Program.cs never references IndemnityPayRule -- see class remarks.
            null);
    }

    /// <summary>
    /// Legacy <c>Program.GetIndemnityPaidDays</c> (lines 3828-3843). Wage code <b>4005</b>. Sums
    /// <c>EmployeeGratuity.GratuityPaidDays</c> for gratuity payments made inside the calendar month of
    /// <c>payDate</c>. Note it uses the CALENDAR month, ignoring <c>offCycleEndDate</c> entirely.
    /// </summary>
    private double GetIndemnityPaidDays(PayrollCalculationContext context)
    {
        var payStartDate = new DateTime(context.PayDate.Year, context.PayDate.Month, 1, 0, 0, 0);
        var payEndDate = context.PayDate;

        return _employeeGratuityRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.GratuityPaidDate >= payStartDate
                        && r.GratuityPaidDate <= payEndDate)
            .Sum(r => r.GratuityPaidDays);
    }

    /// <summary>
    /// Legacy <c>Program.GetTotalIndemnityPaidDays</c> (lines 3844-3871). Wage code <b>4026</b>: previous
    /// month's register's <c>4026 + 5026</c> plus this register's <c>4005 + 5005</c>.
    /// </summary>
    private async Task<double> GetTotalIndemnityPaidDaysAsync(PayrollCalculationContext context)
    {
        double daysCount = 0;

        var prevMonthEndDate = MonthEnd(context.PayDate.AddMonths(-1));

        var prevSalReg = context.PreviousSalaryRegisters
            .Where(r => r.ForPeriod == prevMonthEndDate)
            .OrderByDescending(r => r.Id)
            .FirstOrDefault();

        if (prevSalReg != null)
        {
            daysCount = daysCount + await GetLineNumberAsync(prevSalReg, "4026");
            daysCount = daysCount + await GetLineNumberAsync(prevSalReg, "5026");
        }

        daysCount = daysCount + await GetLineNumberAsync(context.Register, "4005");
        daysCount = daysCount + await GetLineNumberAsync(context.Register, "5005");

        return daysCount;
    }

    /// <summary>
    /// Legacy <c>Program.GetIndemnityBalanceDays</c> (lines 3872-3921). Wage code <b>4006</b>.
    ///
    /// <para><c>4025 + 5025</c> minus the paid days. Which paid days depends on whether indemnity has ever
    /// been RESET: if the employee has a <c>ResetGratuityCalc</c> gratuity row on or before
    /// <c>payDate</c>, subtract the gratuity days paid strictly AFTER that reset; otherwise subtract this
    /// register's 4026. A negative result (only reachable after a mid-period reset) is replaced by
    /// <c>4025 + 5025</c> minus gratuity paid since
    /// <c>GetIndemnityFromDate</c>.</para>
    ///
    /// <para><b>Legacy quirk preserved:</b> the "last indemnity paid date" lookup at line 3887 orders
    /// <c>ASCENDING</c> and takes the FIRST -- i.e. it is really the EARLIEST reset, not the latest, despite
    /// the variable name. Reproduced exactly.</para>
    /// </summary>
    private async Task<double> GetIndemnityBalanceDaysAsync(PayrollCalculationContext context)
    {
        var payEndDate = context.PayDate;

        double daysCount = 0;
        daysCount = daysCount + await GetLineNumberAsync(context.Register, "4025");
        daysCount = daysCount + await GetLineNumberAsync(context.Register, "5025");

        // Legacy lines 3884-3887 -- note the ASCENDING order (legacy's own wording says "last", the code
        // says first). Preserved.
        var lastIndemnityPaidDate = _employeeGratuityRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.ResetGratuityCalc
                        && r.GratuityPaidDate <= payEndDate)
            .OrderBy(r => r.GratuityPaidDate)
            .Select(r => r.GratuityPaidDate)
            .FirstOrDefault();

        if (lastIndemnityPaidDate != default(DateTime))
        {
            daysCount = daysCount - _employeeGratuityRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.GratuityPaidDate > lastIndemnityPaidDate
                            && r.GratuityPaidDate <= payEndDate)
                .Sum(r => r.GratuityPaidDays);
        }
        else
        {
            daysCount = daysCount - await GetLineNumberAsync(context.Register, "4026");
        }

        // Legacy lines 3902-3913: only reachable when indemnity was reset mid-period.
        if (daysCount < 0)
        {
            daysCount = await GetLineNumberAsync(context.Register, "4025");
            daysCount = daysCount + await GetLineNumberAsync(context.Register, "5025");

            var indemnityFromDate = GetIndemnityFromDate(context);

            daysCount = daysCount - _employeeGratuityRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.GratuityPaidDate > indemnityFromDate
                            && r.GratuityPaidDate <= payEndDate)
                .Sum(r => r.GratuityPaidDays);
        }

        return daysCount;
    }

    /// <summary>
    /// Legacy <c>Program.GetIndemnityFromDate</c> (lines 3922-3961): the employee's FIRST contract start
    /// date, unless a <c>ResetGratuityCalc</c> gratuity payment happened on or before <c>payDate</c>, in
    /// which case the LATEST such reset date wins (legacy walks the rows newest-first and breaks on the first
    /// reset).
    ///
    /// <para><b>Legacy quirk preserved:</b> the contract lookup at line 3934 only checks that SOME contract
    /// ends on/after the pay month start; the date actually taken is the earliest contract's
    /// <c>StartDate</c> regardless (line 3938). Legacy's own <c>IndemnityCountFromDate = empContract.StartDate</c>
    /// alternative is commented out at line 3939.</para>
    ///
    /// <para>Ported here rather than reused: <see cref="IIndemnityCalculationService"/> does not expose it,
    /// and <see cref="IIndemnityCalculationService.GetIndemnityBasisDaysAsync"/> answers a different
    /// question.</para>
    /// </summary>
    private DateTime GetIndemnityFromDate(PayrollCalculationContext context)
    {
        var indemnityCountFromDate = new DateTime(1, 1, 1, 0, 0, 0);
        var payStartDate = new DateTime(context.PayDate.Year, context.PayDate.Month, 1, 0, 0, 0);

        var hasContract = _employeeContractRepository.GetAll()
            .Any(r => r.EmployeeId == context.EmployeeId && r.EndDate >= payStartDate);

        if (hasContract)
        {
            indemnityCountFromDate = _employeeContractRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId)
                .OrderBy(r => r.StartDate)
                .Select(r => r.StartDate)
                .FirstOrDefault();
        }

        var reset = _employeeGratuityRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.GratuityPaidDate <= context.PayDate
                        && r.ResetGratuityCalc)
            .OrderByDescending(r => r.GratuityPaidDate)
            .Select(r => r.GratuityPaidDate)
            .FirstOrDefault();

        if (reset != default(DateTime))
        {
            indemnityCountFromDate = reset;
        }

        return indemnityCountFromDate;
    }

    #endregion

    #region << 4027 / 4028 -- Air-ticket accrual family >>

    /// <summary>
    /// Legacy <c>Program.GetEmployeeTicketAccrual</c> (lines 4370-4402). Wage code <b>4027</b>. Sums
    /// <c>TimeRegister.TicketAccrualPerDay</c>. Off-cycle ASSIGNS <c>payEndDate = offCycleEndDate</c>.
    /// </summary>
    private double GetEmployeeTicketAccrual(PayrollCalculationContext context)
    {
        var payStartDate = new DateTime(context.PayDate.Year, context.PayDate.Month, 1, 0, 0, 0);

        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            var rows = context.TimeRegisterList;
            if (rows == null)
            {
                return 0;
            }

            return rows
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= payStartDate
                            && r.DayDate <= context.PayDate)
                .Sum(r => r.TicketAccrualPerDay);
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            var payEndDate = context.OffCycleEndDate;

            return context.TimeRegisterQuery
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= payStartDate
                            && r.DayDate <= payEndDate)
                .Sum(r => r.TicketAccrualPerDay);
        }

        return 0;
    }

    /// <summary>
    /// Legacy <c>Program.GetEmployeeTicketTotalAccrual</c> (lines 4323-4369). Wage code <b>4028</b>:
    /// previous month's register's <c>4028 + 5028</c>, plus this register's <c>4027 + 5027</c>, MINUS the
    /// <c>TimeRegister.AirTicketPaidDay</c> days consumed in this period.
    /// </summary>
    private async Task<double> GetEmployeeTicketTotalAccrualAsync(PayrollCalculationContext context)
    {
        double daysCount = 0;

        var payStartDate = new DateTime(context.PayDate.Year, context.PayDate.Month, 1, 0, 0, 0);
        var prevMonthEndDate = MonthEnd(context.PayDate.AddMonths(-1));

        var prevSalReg = context.PreviousSalaryRegisters
            .Where(r => r.ForPeriod == prevMonthEndDate)
            .OrderByDescending(r => r.Id)
            .FirstOrDefault();

        if (prevSalReg != null)
        {
            daysCount = daysCount + await GetLineNumberAsync(prevSalReg, "4028");
            daysCount = daysCount + await GetLineNumberAsync(prevSalReg, "5028");
        }

        daysCount = daysCount + await GetLineNumberAsync(context.Register, "4027");
        daysCount = daysCount + await GetLineNumberAsync(context.Register, "5027");

        double airTicketPaidDays = 0;

        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            var rows = context.TimeRegisterList;
            if (rows != null)
            {
                airTicketPaidDays = rows
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.DayDate >= payStartDate
                                && r.DayDate <= context.PayDate)
                    .Sum(r => r.AirTicketPaidDay);
            }
        }
        else if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            var payEndDate = context.OffCycleEndDate;

            airTicketPaidDays = context.TimeRegisterQuery
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.DayDate >= payStartDate
                            && r.DayDate <= payEndDate)
                .Sum(r => r.AirTicketPaidDay);
        }

        return daysCount - airTicketPaidDays;
    }

    #endregion

    #region << Shared helpers >>

    /// <summary>
    /// Legacy's recurring gate <c>payrollRunType == Regular || (payrollRunType == OffCycle &amp;&amp;
    /// tempWageType.IncludeInAdvancePay == true &amp;&amp; offCycleReasons != endOfServiceOffcycle)</c>
    /// (legacy lines 2253-2254, 2272-2273, 2291-2292, 2312-2313, 2336-2337, 2388, 2413, 2422, 2449, 2467).
    ///
    /// <para>Legacy resolves the wage type with <c>GetWageType(objectSpace, code)</c>, which returns
    /// <c>null</c> for an unknown code and would then NRE on <c>.IncludeInAdvancePay</c>. A missing wage type
    /// is treated as "not included" here rather than throwing, because the subsequent
    /// <see cref="CalculateTimeTypeAsync"/> call raises the precise
    /// "WageType {code} : {message} not found" message legacy intends -- and only when the number is
    /// non-zero, exactly as legacy.</para>
    /// </summary>
    private Task<bool> AllowAdvancePayAsync(PayrollCalculationContext context, string wageCode, bool notEosOffCycle, bool isRegular)
    {
        if (isRegular)
        {
            return Task.FromResult(true);
        }

        if (!notEosOffCycle)
        {
            return Task.FromResult(false);
        }

        var includeInAdvancePay = _wageTypeRepository.GetAll()
            .Where(r => r.Code == wageCode)
            .Select(r => (bool?)r.IncludeInAdvancePay)
            .FirstOrDefault();

        return Task.FromResult(includeInAdvancePay == true);
    }

    /// <summary>
    /// Legacy's <c>WageTypeList.Where(r =&gt; r.Code == code &amp;&amp; r.IncludeInIndemnity == true).Any()</c>
    /// idiom (legacy lines 4420, 4443, 4505, 4526).
    /// </summary>
    private bool HasWageTypeIncludedInIndemnity(string wageCode)
    {
        return _wageTypeRepository.GetAll().Any(r => r.Code == wageCode && r.IncludeInIndemnity);
    }

    /// <summary>
    /// Legacy's <c>salaryRegister.SalaryRegisterDetail.Where(r =&gt; r.WageType.Code == code)
    /// .Select(r =&gt; r.Number).FirstOrDefault()</c> idiom, which appears ~30 times in this region. A missing
    /// line contributes 0 (legacy's <c>FirstOrDefault()</c> on <c>double</c>).
    /// </summary>
    private async Task<double> GetLineNumberAsync(PayrollRegisterRef register, string wageCode)
    {
        var line = await _registerStore.GetLineAsync(register, wageCode);
        return line?.Number ?? 0;
    }

    /// <summary>Same as <see cref="GetLineNumberAsync(PayrollRegisterRef, string)"/> for another register.</summary>
    private Task<double> GetLineNumberAsync(SalaryRegister salaryRegister, string wageCode)
    {
        return GetLineNumberAsync(ToRegisterRef(salaryRegister), wageCode);
    }

    /// <summary>
    /// Wraps a concrete <see cref="SalaryRegister"/> as a <see cref="PayrollRegisterRef"/> so that OTHER
    /// registers (previous month, retro siblings) are also read through <see cref="IPayrollRegisterStore"/>
    /// -- keeping wave P7 a single-file change. Always <see cref="PayrollRegisterTarget.Live"/>: the lists it
    /// is built from (<c>PreviousSalaryRegisters</c>/<c>CurrentSalaryRegisters</c>) are live entities.
    /// </summary>
    private static PayrollRegisterRef ToRegisterRef(SalaryRegister salaryRegister)
    {
        return new PayrollRegisterRef
        {
            RegisterId = salaryRegister.Id,
            Target = PayrollRegisterTarget.Live,
            ForPeriod = salaryRegister.ForPeriod,
            InPeriod = salaryRegister.InPeriod,
            PayrollRunType = salaryRegister.PayrollRunType
        };
    }

    /// <summary>
    /// Legacy <c>Program.GetCutoffSetting</c>, re-derived here rather than reused -- see
    /// <see cref="GetAbsentUnpaidDaysAsync"/> remarks for why. When the wage type is
    /// <c>IncludeInCutoff</c> and the payroll area defines a non-zero <c>CutOffDay</c>, the window shifts to
    /// [previous month's cut-off day + 1 .. this month's cut-off day].
    /// </summary>
    private (DateTime PayStartDate, DateTime PayEndDate) ApplyCutoffSetting(
        string wageTypeCode,
        DateTime payStartDate,
        DateTime payEndDate,
        PayrollArea payrollArea)
    {
        var includeInCutoff = _wageTypeRepository.GetAll()
            .Where(r => r.Code == wageTypeCode)
            .Select(r => (bool?)r.IncludeInCutoff)
            .FirstOrDefault();

        var cutoffDay = payrollArea?.CutOffDay ?? 0;

        if (includeInCutoff != true || cutoffDay <= 0)
        {
            return (payStartDate, payEndDate);
        }

        var cutoffEnd = CutoffDate(payEndDate, cutoffDay);
        var cutoffStart = CutoffDate(payEndDate.AddMonths(-1), cutoffDay).AddDays(1);

        return (cutoffStart, cutoffEnd);
    }

    /// <summary>The <paramref name="cutoffDay"/>-th day of <paramref name="payDate"/>'s month, clamped to the month's length.</summary>
    private static DateTime CutoffDate(DateTime payDate, int cutoffDay)
    {
        var daysInMonth = DateTime.DaysInMonth(payDate.Year, payDate.Month);
        var day = cutoffDay > daysInMonth ? daysInMonth : cutoffDay;
        return new DateTime(payDate.Year, payDate.Month, day, 0, 0, 0);
    }

    /// <summary>
    /// Resolves legacy's <c>r.LeaveType.Code == "x"</c> against <c>AbsenceType.Code</c>, because this port's
    /// <c>TimeRegister.LeaveTypeId</c> is a bare id with no navigation property (see class remarks).
    /// </summary>
    private int? GetAbsenceTypeIdByCode(string code)
    {
        return _absenceTypeRepository.GetAll()
            .Where(r => r.Code == code)
            .Select(r => (int?)r.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Legacy derived <c>EmployeeGroup</c> from <c>empOrgAssignmentList</c> date-effectively; this port's
    /// <see cref="EmployeeOrgAssignment"/> now carries the FK (migration
    /// <c>20260728103447</c>), so the same lookup runs over
    /// <see cref="PayrollCalculationContext.OrgAssignments"/>.
    /// </summary>
    private static int? GetEmployeeGroupId(PayrollCalculationContext context)
    {
        return context.OrgAssignments
            .Where(a => a.StartDate <= context.PayDate && a.EndDate >= context.PayDate && a.EmployeeGroupId != null)
            .Select(a => a.EmployeeGroupId)
            .FirstOrDefault();
    }

    /// <summary>Sibling of <see cref="GetEmployeeGroupId"/> for <c>EmployeeSubGroup</c>.</summary>
    private static int? GetEmployeeSubGroupId(PayrollCalculationContext context)
    {
        return context.OrgAssignments
            .Where(a => a.StartDate <= context.PayDate && a.EndDate >= context.PayDate && a.EmployeeSubGroupId != null)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefault();
    }

    /// <summary>Last day of <paramref name="date"/>'s month -- legacy's ubiquitous inline month-end expression.</summary>
    private static DateTime MonthEnd(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 0, 0, 0);
    }

    #endregion
}
