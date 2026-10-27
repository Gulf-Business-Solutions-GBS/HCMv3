using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IEmployeeAttendancePayCalculationService"/>
/// <remarks>
/// <b>WAVE P9.</b> Ported from legacy <c>HCM10.PayrollEngine/EmployeeAttendancePayService.cs</c>:
/// <c>Main</c> (legacy line 14) and its private <c>GetAttendancePay</c> (legacy line 103).
/// <c>Main_Simu</c>/<c>GetAttendancePay_Simu</c> (legacy lines 204-386) are deliberately NOT ported -- wave P7
/// collapses the simulation clone through <see cref="PayrollRegisterRef.Target"/>.
///
/// <para>Dispatched at legacy <c>Program.cs</c> line 2585, the LAST arm of the chain. Legacy applies no
/// on-hold gate on this arm, so none is applied here either.</para>
///
/// <para><b>Preserved legacy quirks -- all deliberate, none "fixed":</b></para>
/// <list type="number">
/// <item><description><b>The <c>IsNotReturnFromLeave</c> arm is EMPTY and swallows the calculation.</b> Legacy
/// line 26 is <c>if (IsNotReturnFromLeave) { /* body commented out */ } else { ... }</c>. Note it does NOT
/// carry the <c>BillingType != Provision</c> escape that
/// <see cref="PayrollArrearCalculationService"/>'s copy of the same idiom has (legacy
/// <c>PayrollArrearService.cs</c> line 22) -- an inconsistency between the two legacy files, reproduced as
/// found.</description></item>
/// <item><description><b>The off-cycle attendance-day sum is NOT filtered by attendance type.</b> Legacy lines
/// 62-64 sum <c>AttendanceDay</c> over every <c>TimeRegister</c> row inside the matched
/// <c>EmployeeAttendance</c> date span, with no <c>AttendanceType</c> predicate -- unlike the regular arm
/// (legacy line 49), which does filter. Almost certainly a legacy defect; reproduced verbatim because fixing
/// it changes pay. <b>Flagged for the business.</b></description></item>
/// <item><description><b><c>includeInPaidLeaveWageTypes</c> is a misnomer.</b> Legacy lines 121 and 140 name
/// the variable after "paid leave" but the predicate is <c>r.IncludeInBasicPay</c>. The predicate is what was
/// ported; the name is not.</description></item>
/// <item><description><b>The off-cycle <c>empAttendances</c> lookup has NO date filter and matches on
/// attendance-type CODE, not id</b> (legacy lines 150-151), unlike the outer loop's lookup which filters on
/// dates and matches by object (legacy lines 55-58). Both reproduced as written; the code match means two
/// attendance types sharing a code would both be picked up.</description></item>
/// <item><description><b>Division by <c>WageType.FixedDays</c> is unguarded</b> (legacy lines 128 and 147). A
/// wage type configured with <c>FixedDays == 0</c> yields <c>Infinity</c>/<c>NaN</c> in double arithmetic
/// exactly as it does in legacy. Not guarded here -- a guard would silently substitute a different
/// number.</description></item>
/// <item><description><b>Zero DOES delete here.</b> Legacy line 83 calls <c>salaryRegisterDetail.Delete()</c>
/// when the computed amount is 0, so <see cref="IPayrollRegisterStore.SaveLineAsync"/> is called with the
/// DEFAULT <c>deleteWhenAmountIsZero: true</c> -- the opposite of the arrear service, deliberately.</description></item>
/// <item><description><b><c>Rate</c>/<c>Number</c>/<c>Amount</c> accumulate across attendance types onto ONE
/// line.</b> When several attendance types map to the same wage type, legacy re-reads the register detail each
/// iteration and OVERWRITES <c>Rate</c>/<c>Amount</c>/<c>Number</c> (legacy lines 124/191-192) rather than
/// summing, so the last attendance type with <c>attdays &gt; 0</c> wins. Reproduced.</description></item>
/// </list>
///
/// <para><b>No EF Core.</b> Plain synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c>.</para>
/// </remarks>
public class EmployeeAttendancePayCalculationService : HCMv3DomainServiceBase, IEmployeeAttendancePayCalculationService, ITransientDependency
{
    private readonly IRepository<AttendanceType> _attendanceTypeRepository;
    private readonly IRepository<EmployeeAttendance> _employeeAttendanceRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;

    public EmployeeAttendancePayCalculationService(
        IRepository<AttendanceType> attendanceTypeRepository,
        IRepository<EmployeeAttendance> employeeAttendanceRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<WageType> wageTypeRepository,
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService)
    {
        _attendanceTypeRepository = attendanceTypeRepository;
        _employeeAttendanceRepository = employeeAttendanceRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _wageTypeRepository = wageTypeRepository;
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
    }

    /// <summary>Legacy <c>EmployeeAttendancePayService.Main</c> (legacy line 14).</summary>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        if (context?.WageType == null)
        {
            throw new ArgumentException("PayrollCalculationContext.WageType must be set.", nameof(context));
        }

        // Legacy line 26 -- empty if-arm, see class remarks quirk 1.
        if (context.IsNotReturnFromLeave)
        {
            return;
        }

        // Legacy line 35 over Program.AttendanceTypeList, which is pre-filtered to `WageType != null`
        // (legacy Program.cs line 2669).
        var attendanceTypes = _attendanceTypeRepository.GetAll()
            .Where(r => r.WageTypeId != null && r.WageTypeId == context.WageType.Id)
            .ToList();

        foreach (var attendanceType in attendanceTypes)
        {
            double attendanceDays = 0;

            // Legacy lines 40-41.
            var payStartDate = new DateTime(context.PayDate.Year, context.PayDate.Month, 1);
            var payEndDate = context.PayDate;

            if (context.PayrollRunType == PayrollRunType.Regular)
            {
                // Legacy lines 45-50. empTimeRegisterList is never null on a regular run (legacy line 1614).
                attendanceDays = (context.TimeRegisterList ?? new List<TimeRegister>())
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.DayDate >= payStartDate
                                && r.DayDate <= payEndDate
                                && r.AttendanceTypeId != null
                                && r.AttendanceTypeId == attendanceType.Id)
                    .Sum(r => r.AttendanceDay);
            }
            else if (context.PayrollRunType == PayrollRunType.OffCycle)
            {
                // Legacy lines 54-65.
                payEndDate = context.OffCycleEndDate;

                var employeeAttendances = _employeeAttendanceRepository.GetAll()
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.AttendanceTypeId == attendanceType.Id
                                && r.StartDate >= payStartDate
                                && r.StartDate <= payEndDate)
                    .Select(r => new { r.StartDate, r.EndDate })
                    .ToList();

                foreach (var attendance in employeeAttendances)
                {
                    // QUIRK: no AttendanceType predicate here -- see class remarks quirk 2.
                    attendanceDays += TimeRegisterQuery(context)
                        .Where(r => r.EmployeeId == context.EmployeeId
                                    && r.DayDate >= attendance.StartDate
                                    && r.DayDate <= attendance.EndDate)
                        .Sum(r => r.AttendanceDay);
                }
            }

            // Legacy line 68.
            if (attendanceDays <= 0)
            {
                continue;
            }

            // Legacy lines 70-79.
            var line = await _registerStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id)
                       ?? new PayrollRegisterLine
                       {
                           WageTypeId = context.WageType.Id,
                           WageCode = context.WageType.Code
                       };

            await GetAttendancePayAsync(context, attendanceType, attendanceDays, line);

            // Legacy lines 82-93: Amount == 0 deletes the row, otherwise it is added.
            await _registerStore.SaveLineAsync(context.Register, line);
        }
    }

    /// <summary>
    /// Legacy <c>EmployeeAttendancePayService.GetAttendancePay</c> (legacy line 103). Mutates
    /// <paramref name="line"/> in place, exactly as legacy mutates its <c>SalaryRegisterDetail</c>.
    /// </summary>
    private async Task GetAttendancePayAsync(
        PayrollCalculationContext context,
        AttendanceType attendanceType,
        double attendanceDays,
        PayrollRegisterLine line)
    {
        double amount = 0;

        // Legacy line 111.
        if (attendanceDays <= 0)
        {
            return;
        }

        var wageType = context.WageType;

        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            // Legacy lines 118-129.
            var totalBasicPay = await GetIncludeInBasicPayRateAsync(context);
            line.Rate = totalBasicPay;
            amount = (totalBasicPay / wageType.FixedDays) * attendanceDays;
        }
        else if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            // Legacy lines 136-138.
            var allowed = (context.IsAdvanceLeave && wageType.IncludeInAdvancePay) || context.IsEndOfService;

            if (allowed)
            {
                // Legacy lines 140-148.
                var totalBasicPay = await GetIncludeInBasicPayRateAsync(context);
                line.Rate = totalBasicPay;
                amount = (totalBasicPay / wageType.FixedDays) * attendanceDays;

                // Legacy lines 150-151: NO date filter, matched by CODE -- see class remarks quirk 4.
                var employeeAttendances = _employeeAttendanceRepository.GetAll()
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.AttendanceType != null
                                && r.AttendanceType.Code == attendanceType.Code)
                    .Select(r => new { r.StartDate, r.EndDate })
                    .ToList();

                // Legacy lines 153-155.
                double attendanceAmountPerDay = 0;
                if (amount != 0 && attendanceDays > 0)
                {
                    attendanceAmountPerDay = amount / attendanceDays;
                }

                // Legacy line 160. NOTE the arguments are (payDate, offCycleEndDate) -- legacy computes
                // startDate/endDate locals on lines 158-159 and then never uses them.
                var recoveryPeriods = _advancePaymentService.GetRecoveryPeriods(context.PayDate, context.OffCycleEndDate);

                foreach (var recoveryPeriod in recoveryPeriods)
                {
                    // Legacy lines 166-176.
                    double leaveDaysInMonth = 0;
                    var monthEnd = PayrollRunGates.MonthEnd(recoveryPeriod);

                    foreach (var attendance in employeeAttendances)
                    {
                        leaveDaysInMonth += TimeRegisterQuery(context)
                            .Count(r => r.EmployeeId == context.EmployeeId
                                        && r.DayDate >= attendance.StartDate
                                        && r.DayDate <= attendance.EndDate
                                        && r.DayDate >= recoveryPeriod
                                        && r.DayDate <= monthEnd
                                        && r.AttendanceDay == 1);
                    }

                    // Legacy lines 178-184.
                    if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
                    {
                        await AdvancePaymentDedAsync(context, recoveryPeriod, attendanceAmountPerDay * leaveDaysInMonth);
                    }
                }
            }
        }

        // Legacy lines 191-192 -- written on BOTH arms, including the off-cycle arm that computed nothing
        // (in which case amount stays 0 and the caller deletes the row).
        line.Amount = amount;
        line.Number = attendanceDays;
    }

    /// <summary>
    /// Legacy lines 121-123 / 140-142: the sum of <c>Rate</c> over every register line whose wage type has
    /// <see cref="WageType.IncludeInBasicPay"/>. (Legacy calls the local variable
    /// <c>includeInPaidLeaveWageTypes</c> -- see class remarks quirk 3.)
    /// </summary>
    private async Task<double> GetIncludeInBasicPayRateAsync(PayrollCalculationContext context)
    {
        var includeInBasicPayWageTypeIds = _wageTypeRepository.GetAll()
            .Where(r => r.IncludeInBasicPay)
            .Select(r => r.Id)
            .ToList();

        var lines = await _registerStore.GetLinesAsync(context.Register);

        return lines.Where(l => includeInBasicPayWageTypeIds.Contains(l.WageTypeId)).Sum(l => l.Rate);
    }

    /// <summary>
    /// Legacy <c>empTimeRegister</c> -- the UNFILTERED whole-table query (legacy
    /// <c>Program.GetEmployeeTimeRegister</c>, legacy <c>Program.cs</c> lines 2713-2716). Falls back to the
    /// repository when the context did not carry one, matching
    /// <see cref="EmployeeBasicPayCalculationService"/>'s handling.
    /// </summary>
    private IQueryable<TimeRegister> TimeRegisterQuery(PayrollCalculationContext context)
    {
        return context.TimeRegisterQuery ?? _timeRegisterRepository.GetAll();
    }

    /// <summary>
    /// Legacy <c>Program.AdvancePaymentDed</c> (legacy <c>Program.cs</c> line 2940), reached from legacy
    /// <c>EmployeeAttendancePayService.cs</c> line 182. Legacy's two early exits (zero amount, End-Of-Service
    /// off-cycle) live in <see cref="IPayrollAdvancePaymentService"/>; they are repeated here for the same
    /// reason <see cref="EmployeeBasicPayCalculationService"/> repeats them.
    /// </summary>
    private Task AdvancePaymentDedAsync(PayrollCalculationContext context, DateTime paymentDate, double amount)
    {
        if (amount == 0)
        {
            return Task.CompletedTask;
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle && context.IsEndOfService)
        {
            return Task.CompletedTask;
        }

        return _advancePaymentService.AdvancePaymentDedAsync(
            context.EmployeeId,
            paymentDate,
            context.OffCyclePaymentDate,
            amount,
            context.PayrollRunType,
            context.OffCycleReason,
            context.AdvancePayComment,
            context.Settings);
    }
}
