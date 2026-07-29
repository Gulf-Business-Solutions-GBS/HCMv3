using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The wage-type loop half of <see cref="PayrollEngineService"/> -- the port of legacy
/// <c>HCM10.PayrollEngine/Program.RunPayrollCalculation</c> (legacy <c>Program.cs</c> lines 1597-2601).
/// </summary>
public partial class PayrollEngineService
{
    /// <summary>
    /// Builds the per-(employee, period) <see cref="PayrollCalculationContext"/> and runs every active
    /// non-TimeType wage type against it, in <see cref="WageType.PayrollOrder"/> order.
    ///
    /// <para><b>Ordering is load-bearing, not cosmetic</b> (legacy line 2524 orders by
    /// <c>PayrollOrder</c>):</para>
    /// <list type="bullet">
    /// <item><description>9000 Net Pay is a roll-up over every other line, so it must be LAST -- which the
    /// data guarantees by giving it the highest <c>PayrollOrder</c>. It also calls
    /// <see cref="IPayrollRoundingService.ApplyRoundingAsync"/> before summing (inside its own calculator,
    /// legacy <c>EmployeeCalculatedPayService.cs</c> line 1062).</description></item>
    /// <item><description>Codes 11, 13 and 23 read net pay MID-RUN as a deduction budget (legacy lines 3302,
    /// 3479, 3724), so their results depend on which lines already exist. Code 23 additionally calls
    /// <c>ApplyRoundingAsync</c> mid-run (legacy line 3723) -- the register is legitimately rounded twice per
    /// run.</description></item>
    /// </list>
    ///
    /// <para><b>Wage-type error wrapping</b> -- legacy lines 2594-2599. Any exception is re-thrown with
    /// "Error in calculating WageType '{code} : {name}'" appended, and the caller's per-employee
    /// <c>catch</c> logs it and moves to the next employee.</para>
    /// </summary>
    private async Task RunPayrollCalculationAsync(
        PayrollEngineRunInput input,
        Employee employee,
        DateTime payDate,
        DateTime payInDate,
        PayrollEngineSettings settings,
        PayrollOnHoldFlags onHold,
        bool isNotReturnFromLeave,
        PayrollArea payrollArea,
        List<EmployeeOrgAssignment> orgAssignments,
        PayrollRegisterRef register)
    {
        var currentWageCodeForError = string.Empty;

        try
        {
            // Legacy lines 1606-1607.
            var payStartDate = new DateTime(payDate.Year, payDate.Month, 1, 0, 0, 0);
            var offCycleEndDate = payInDate.Date;
            var advancePayComment = string.Empty;

            List<TimeRegister> timeRegisterList;

            if (input.PayrollRunType == PayrollRunType.Regular)
            {
                // Legacy lines 1614-1616.
                timeRegisterList = _timeRegisterRepository.GetAll()
                    .Where(r => r.EmployeeId == employee.Id && r.DayDate >= payStartDate && r.DayDate <= payDate)
                    .ToList();

                // NOT PORTED: the "Clear EmployeePayment Advance recovery" block (legacy lines 1619-1653) --
                // its predicate keys off EmployeePayment.OffCyclePaymentDate, which this port does not carry.

                EnsureTimeRegisterApproved(settings, employee, payStartDate, payInDate);
            }
            else
            {
                offCycleEndDate = ResolveOffCycleEndDate(input, employee, payStartDate, offCycleEndDate, ref advancePayComment);

                // Legacy line 1771: empTimeRegisterList is deliberately NULL on an off-cycle run. Rules must
                // null-check it exactly where legacy did -- preserved.
                timeRegisterList = null;

                EnsureTimeRegisterApproved(settings, employee, payStartDate, offCycleEndDate);

                // NOT PORTED: the 2005/2011 clearing blocks (legacy lines 1787-1851) -- see above.
            }

            // Legacy lines 1875-1878.
            var paymentList = _employeePaymentRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id
                            && r.PaymentDate >= payStartDate
                            && r.PaymentDate <= offCycleEndDate)
                .ToList();

            // Legacy lines 1900-1905.
            var prevMonthEndDate = PayrollControlRecordGuard.MonthEnd(payDate.AddMonths(-1));
            var previousRegisters = _salaryRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id
                            && r.ForPeriod == prevMonthEndDate
                            && r.PayrollRunType == input.PayrollRunType)
                .ToList();

            // Legacy lines 1907-1912.
            var currentRegisters = _salaryRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id
                            && r.ForPeriod != payInDate
                            && r.InPeriod == payInDate
                            && r.PayrollResultType == PayrollResultType.Current
                            && r.PayrollRunType == input.PayrollRunType)
                .OrderBy(r => r.ForPeriod)
                .ToList();

            var context = new PayrollCalculationContext
            {
                EmployeeId = employee.Id,
                Employee = employee,
                PayDate = payDate,
                PayStartDate = payStartDate,
                PayInDate = payInDate,
                OffCycleEndDate = offCycleEndDate,
                OffCyclePaymentDate = input.OffCyclePaymentDate,
                PayrollRunType = input.PayrollRunType,
                OffCycleReason = input.OffCycleReason,
                Register = register,
                TimeRegisterList = timeRegisterList,
                // Legacy Program.GetEmployeeTimeRegister (lines 2713-2716) IGNORES all four of its arguments
                // and returns the UNFILTERED table query. Preserved deliberately -- ported rules re-apply
                // their own predicates and several deliberately read outside the pay period.
                TimeRegisterQuery = _timeRegisterRepository.GetAll(),
                PaymentList = paymentList,
                OrgAssignments = orgAssignments,
                PreviousSalaryRegisters = previousRegisters,
                CurrentSalaryRegisters = currentRegisters,
                OnHold = onHold,
                IsNotReturnFromLeave = isNotReturnFromLeave,
                PayrollArea = payrollArea,
                AdvancePayComment = advancePayComment,
                Settings = settings
            };

            await RunTimeTypeCumulationAsync(context);

            var categories = await BuildWageTypeCategoriesAsync(employee, payDate, payStartDate, offCycleEndDate, paymentList);

            // Legacy line 2524: EVERY active non-TimeType wage type, ordered by PayrollOrder. Note the
            // "only wage types this employee actually uses" filter is commented out in legacy (line 2523)
            // and therefore genuinely absent -- unclaimed codes simply fall through the dispatch chain.
            var wageTypes = _wageTypeRepository.GetAll()
                .Where(r => r.Status && r.BillingType != BillingType.TimeType)
                .OrderBy(r => r.PayrollOrder)
                .ToList();

            foreach (var wageType in wageTypes)
            {
                currentWageCodeForError = wageType.Code + " : " + wageType.NameEn;
                context.WageType = wageType;

                // Legacy line 2530 -- Program.Allow_CalcWageType, applied ONCE around the whole dispatch
                // chain. (IWageTypeCalculatorResolver applies its own copy as gate 1 on the calculated-pay
                // arm; evaluating it twice there is idempotent.)
                if (!await AllowCalcWageTypeAsync(context))
                {
                    continue;
                }

                await DispatchWageTypeAsync(context, categories);
            }
        }
        catch (Exception ex)
        {
            // Legacy lines 2594-2599, verbatim message shape.
            var suffix = currentWageCodeForError != string.Empty
                ? "Error in calculating WageType '" + currentWageCodeForError + "' "
                : string.Empty;

            throw new UserFriendlyException(ex.Message + "\r\n" + suffix);
        }
    }

    #region << Category dispatch (legacy lines 2532-2589) >>

    /// <summary>
    /// The per-employee wage-type id sets legacy rebuilds at lines 1918-1942 plus the run-wide leave-pay set
    /// (lines 2675-2676). Category dispatch stays in the engine precisely because these sets are per-employee
    /// runtime data that <see cref="IWageTypeCalculator.WageCode"/> cannot express.
    /// </summary>
    private sealed class WageTypeCategories
    {
        public HashSet<int> BasicPay = new HashSet<int>();
        public HashSet<int> LeavePay = new HashSet<int>();
        public HashSet<int> Payment = new HashSet<int>();
        public HashSet<int> RecurringPayment = new HashSet<int>();
        public HashSet<int> Overtime = new HashSet<int>();
        public HashSet<int> Attendance = new HashSet<int>();
    }

    private async Task<WageTypeCategories> BuildWageTypeCategoriesAsync(
        Employee employee,
        DateTime payDate,
        DateTime payStartDate,
        DateTime offCycleEndDate,
        List<EmployeePayment> paymentList)
    {
        var categories = new WageTypeCategories();

        // Legacy lines 1918-1931 -- shipped by wave P4 as an exact port of that list expression.
        foreach (var id in await _basicPayService.GetBasicPayWageTypeIdsAsync(employee.Id, payDate))
        {
            categories.BasicPay.Add(id);
        }

        // Legacy lines 2675-2676 -- likewise.
        foreach (var id in await _leavePayService.GetLeavePayWageTypeIdsAsync())
        {
            categories.LeavePay.Add(id);
        }

        // Legacy line 1932 -- ACTIVE since wave P8 restored GeneralMasterInfo.OvertimeType.WageTypeId:
        //     wtOverTimeWageList = EmployeeOvertime.Where(r => r.Employee.Oid == emp.Oid
        //                                                   && r.EffectiveDate >= payStartDate
        //                                                   && r.EffectiveDate <= offCycleEndDate
        //                                                   && r.OvertimeType != null)
        //                                          .Select(r => r.OvertimeType.WageType.Oid).ToList();
        // Legacy's `r.OvertimeType != null` guard becomes `r.OvertimeType.WageTypeId != null` here: this
        // port's EmployeeOvertime.OvertimeTypeId is a required FK, so the OvertimeType is always present,
        // and the value that can be absent is its (nullable) WageTypeId.
        foreach (var id in _employeeOvertimeRepository.GetAll()
                     .Where(r => r.EmployeeId == employee.Id
                                 && r.EffectiveDate >= payStartDate
                                 && r.EffectiveDate <= offCycleEndDate
                                 && r.OvertimeType != null
                                 && r.OvertimeType.WageTypeId != null)
                     .Select(r => r.OvertimeType.WageTypeId.Value)
                     .ToList())
        {
            categories.Overtime.Add(id);
        }

        // Legacy line 1936.
        foreach (var id in _employeeAttendanceRepository.GetAll()
                     .Where(r => r.EmployeeId == employee.Id
                                 && !(r.StartDate > offCycleEndDate || r.EndDate < payStartDate)
                                 && r.AttendanceType != null
                                 && r.AttendanceType.WageTypeId != null)
                     .Select(r => r.AttendanceType.WageTypeId.Value)
                     .ToList())
        {
            categories.Attendance.Add(id);
        }

        // Legacy lines 1939-1940.
        foreach (var id in paymentList.Select(r => r.WageTypeId))
        {
            categories.Payment.Add(id);
        }

        foreach (var id in _employeeSubGroupPaymentRepository.GetAll()
                     // Legacy's enum member is spelt "Yealy"; this port fixed the typo to "Yearly".
                     .Where(r => (r.PayInterval == PayInterval.EndOfService || r.PayInterval == PayInterval.Yearly)
                                 && r.WageTypeId != null)
                     .Select(r => r.WageTypeId.Value)
                     .ToList())
        {
            categories.Payment.Add(id);
        }

        // Legacy line 1942.
        foreach (var id in _employeeRecurringPaymentRepository.GetAll()
                     .Where(r => r.EmployeeId == employee.Id
                                 && !(r.StartDate > offCycleEndDate || r.EndDate < payStartDate))
                     .Select(r => r.WageTypeId)
                     .ToList())
        {
            categories.RecurringPayment.Add(id);
        }

        return categories;
    }

    /// <summary>
    /// Legacy's else-if chain at lines 2532-2589, in ITS EXACT ORDER (arrear wins over basic pay, which wins
    /// over payment, ...). The two category-level on-hold gates that
    /// <see cref="IWageTypeCalculatorResolver"/> does NOT apply are applied here, exactly where legacy has
    /// them:
    /// <list type="bullet">
    /// <item><description><b>Leave pay</b>: <c>if (!onHold_LeavePay)</c> (legacy line 2557).</description></item>
    /// <item><description><b>Payments</b>: the earning/deduction PAIR test (legacy line 2547).</description></item>
    /// <item><description><b>Overtime</b>: <c>if (!onHold_AllEarnings)</c> (legacy line 2567).</description></item>
    /// </list>
    ///
    /// <para><b>Basic pay is deliberately NOT gated on <c>onHold_BasicPay</c>.</b> That flag works the
    /// OPPOSITE way: legacy passes it INTO the service, where it suppresses Number/Amount but STILL WRITES
    /// RATE (legacy <c>EmployeeBasicPayService.cs</c> lines 224-228). Gating basic pay out on it would drop
    /// the rate row. The flag reaches the service through
    /// <see cref="PayrollCalculationContext.OnHold"/>.</para>
    ///
    /// <para>Basic pay, leave pay, payments, overtime, attendance and arrears also bypass
    /// <see cref="IWageTypeCalculatorResolver"/> entirely -- legacy applies NO <c>IsOnHold</c>/
    /// <c>AllowToCalculate</c>/<c>IsNotReturnFromLeave</c> gate on those arms, so routing them through the
    /// resolver would be ADDING behaviour.</para>
    /// </summary>
    private async Task DispatchWageTypeAsync(PayrollCalculationContext context, WageTypeCategories categories)
    {
        var wageType = context.WageType;

        // Legacy line 2532.
        if (wageType.IncludeInArrearPay)
        {
            await ArrearService.CalculateAsync(context);
            return;
        }

        // Legacy line 2538.
        if (categories.BasicPay.Contains(wageType.Id))
        {
            await _basicPayService.CalculateAsync(context);
            return;
        }

        // Legacy lines 2545-2554.
        if (categories.Payment.Contains(wageType.Id) || categories.RecurringPayment.Contains(wageType.Id))
        {
            var allowed = (!context.OnHold.AllEarnings && wageType.BillingType == BillingType.Earning)
                          || (!context.OnHold.AllDeductions && wageType.BillingType == BillingType.Deduction);

            if (allowed)
            {
                await PaymentService.CalculateAsync(context);
            }

            return;
        }

        // Legacy lines 2555-2564.
        if (categories.LeavePay.Contains(wageType.Id))
        {
            if (!context.OnHold.LeavePay)
            {
                await _leavePayService.CalculateAsync(context);
            }

            return;
        }

        // Legacy lines 2565-2573.
        if (categories.Overtime.Contains(wageType.Id))
        {
            if (!context.OnHold.AllEarnings)
            {
                await OvertimePayService.CalculateAsync(context);
            }

            return;
        }

        // Legacy lines 2574-2582: the calculated-pay arm -- the ONLY arm that goes through the resolver.
        // Legacy's wtCalculatedWageList (built at lines 2698-2702) is exactly "the codes some rule claims",
        // which is what IWageTypeCalculatorResolver.Resolve answers, so membership is not re-derived here.
        if (_wageTypeCalculatorResolver.Resolve(wageType.Code) != null)
        {
            await _wageTypeCalculatorResolver.TryCalculateAsync(context);
            return;
        }

        // Legacy lines 2583-2589.
        if (categories.Attendance.Contains(wageType.Id))
        {
            await AttendancePayService.CalculateAsync(context);
        }
    }

    #endregion

    #region << Gates and helpers >>

    /// <summary>
    /// Legacy <c>Program.Allow_CalcWageType</c> (lines 2752-2770): when this wage type has
    /// <see cref="WageTypeException"/> rows, skip it if ANY excepted wage type already has a line on this
    /// register with a non-zero Rate, Number OR Amount.
    ///
    /// <para><b>Why this is a second copy.</b> <see cref="WageTypeCalculatorResolver"/> has the identical
    /// rule as its gate 1, but private, and that file is fixed. Legacy applies the gate at line 2530 --
    /// around the WHOLE dispatch chain, not just the calculated-pay arm -- so the engine needs its own. The
    /// two must stay in sync; if the rule ever changes, change both.</para>
    ///
    /// <para>Register lines are read through <see cref="IPayrollRegisterStore"/> rather than
    /// <see cref="SalaryRegisterDetail"/> so that wave P7 stays a parameter flip.</para>
    /// </summary>
    private async Task<bool> AllowCalcWageTypeAsync(PayrollCalculationContext context)
    {
        var exceptWageTypeIds = _wageTypeExceptionRepository.GetAll()
            .Where(e => e.WageTypeId == context.WageType.Id)
            .Select(e => e.ExceptWageTypeId)
            .ToList();

        if (!exceptWageTypeIds.Any() || context.Register == null)
        {
            return true;
        }

        var lines = await _registerStore.GetLinesAsync(context.Register);

        return !lines.Any(l => exceptWageTypeIds.Contains(l.WageTypeId)
                               && (l.Rate != 0 || l.Number != 0 || l.Amount != 0));
    }

    /// <summary>
    /// Legacy's "Approved fields" regions (lines 1656-1670 for Regular, 1776-1785 for OffCycle): when
    /// <c>ManageTimeSettings.EnableTimeRegisterApproval</c> is on, ANY unapproved TimeRegister row in the pay
    /// window fails the employee.
    /// </summary>
    private void EnsureTimeRegisterApproved(PayrollEngineSettings settings, Employee employee, DateTime startDate, DateTime endDate)
    {
        if (!settings.EnableTimeRegisterApproval)
        {
            return;
        }

        var hasUnapproved = _timeRegisterRepository.GetAll()
            .Any(r => r.EmployeeId == employee.Id
                      && r.DayDate >= startDate
                      && r.DayDate <= endDate
                      && r.Status != TimeRegisterStatus.Approved);

        if (hasUnapproved)
        {
            throw new UserFriendlyException(string.Format(
                "Time register records with status not approved found for Employee : {0} between '{1}' and '{2}'",
                employee.Code,
                startDate.ToString("dd/MM/yyyy"),
                endDate.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>
    /// Legacy's off-cycle "narrow offCycleEndDate per reason" block (lines 1674-1768), plus the
    /// advance-payment comment built at lines 1710-1734.
    /// </summary>
    private DateTime ResolveOffCycleEndDate(
        PayrollEngineRunInput input,
        Employee employee,
        DateTime payStartDate,
        DateTime offCycleEndDate,
        ref string advancePayComment)
    {
        var offCyclePaymentDate = input.OffCyclePaymentDate;

        if (input.OffCycleReason == PayrollOffCycleReasons.EndOfService)
        {
            // Legacy lines 1678-1695.
            var eos = _employeeFnFPaymentRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeId == employee.Id && r.CompensationDate == offCyclePaymentDate);

            if (eos == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Full and Final Payment Compensation Date entry not found for offcycle payment for Employee : {0} on offCycle Payment Date",
                    employee.Code));
            }

            var separation = _employeeSeparationRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id && r.RevokeDate == null)
                .OrderByDescending(r => r.SubmissionDate)
                .FirstOrDefault();

            if (separation == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Separation entry not found for offcycle payment for Employee : {0} on offCycle Payment Date",
                    employee.Code));
            }

            return separation.LastDayInService < eos.CompensationDate
                ? eos.CompensationDate.Date
                : separation.LastDayInService.Date;
        }

        if (input.OffCycleReason == PayrollOffCycleReasons.AdvanceLeave)
        {
            // Legacy lines 1699-1750.
            var absences = _employeeAbsenceRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id
                            && r.AbsenceType.IsPaid
                            && r.LeavePaymentType == LeavePaymentType.PayInAdvance
                            && r.PaymentDate == offCyclePaymentDate)
                .OrderByDescending(r => r.EndDate)
                .ToList();

            if (absences.Any())
            {
                // Legacy lines 1724-1733. NOT PORTED: the "RequestNumber" fragment -- this port's
                // EmployeeAbsence carries no WfRequest FK.
                advancePayComment = "Advance Payment for leave.  " + Environment.NewLine
                                    + string.Join(" | ", absences.Select(r =>
                                        $"LeaveType : {r.AbsenceType?.NameEn},  "
                                        + $"StartDate : {r.StartDate:dd/MM/yyyy},  "
                                        + $"EndDate : {r.EndDate:dd/MM/yyyy},  "
                                        + $"TotalLeaveDays : {r.TotalLeaveDays},  "
                                        + $"CalendarDays : {r.CalendarDays}  "));

                return absences[0].EndDate.Date;
            }

            var adjustment = GetAdvancePaidEncashmentAdjustment(employee.Id, offCyclePaymentDate);
            if (adjustment == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Leaves not found for offcycle payment for Employee :{0}", employee.Code));
            }

            return adjustment.EffectiveDate.Date;
        }

        if (input.OffCycleReason == PayrollOffCycleReasons.LeaveEncashment)
        {
            // Legacy lines 1754-1767.
            var adjustment = GetAdvancePaidEncashmentAdjustment(employee.Id, offCyclePaymentDate);
            if (adjustment == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Leave encashment not found for offcycle payment for Employee :{0}", employee.Code));
            }

            return adjustment.EffectiveDate.Date;
        }

        return offCycleEndDate;
    }

    /// <summary>
    /// Legacy's advance-paid annual-leave encashment lookup, repeated at lines 1738-1744 and 1756-1762:
    /// scoped to the CALENDAR MONTH of <paramref name="offCyclePaymentDate"/>, latest effective date first.
    /// </summary>
    private EmployeeAbsAdjustment GetAdvancePaidEncashmentAdjustment(int employeeId, DateTime offCyclePaymentDate)
    {
        var monthStart = new DateTime(offCyclePaymentDate.Year, offCyclePaymentDate.Month, 1);
        var monthEnd = PayrollControlRecordGuard.MonthEnd(offCyclePaymentDate);

        return _employeeAbsAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && (r.AbsenceType.Code == "1" || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                        && r.EffectiveDate >= monthStart
                        && r.EffectiveDate <= monthEnd
                        && (r.AdjustmentType == AdjustmentType.Encashment || r.AdjustmentType == AdjustmentType.Fractional)
                        && r.AdjustmentPaymentType == AdjustmentPaymentType.PayInAdvance)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// <b>NOT PORTED -- deliberate, reported gap.</b> Legacy's "All TimeTypes Balance Cumulation" region
    /// (<c>Program.cs</c> lines 1956-2518) makes ~50 <c>CalculateTimeType</c> calls that write the
    /// <c>Number</c> of wage codes 4000-4031 and their 5000-5031 arrear twins BEFORE the main wage-type loop
    /// runs, because most downstream wage rules read those numbers back off the register.
    ///
    /// <para>Only NINE of those ~50 day counts exist in this port
    /// (<see cref="IPayrollDayCountService"/>); the rest (<c>GetAnnualLeavePaidDays</c>,
    /// <c>GetLieuPaidDays</c>, <c>GetInactiveDays</c>, <c>GetIndemnityDays</c>,
    /// <c>GetEmployeeTicketAccrual</c>, <c>GetArrear</c>, <c>UpdateEmployeeIndemnityQuota</c>, ...) have no
    /// port at all. Writing the available subset would produce a register that LOOKS complete while silently
    /// under-reporting ~41 day counts -- exactly the "plausible-looking guess" this port forbids.</para>
    ///
    /// <para><b>It is a no-op rather than a throw</b> because, unlike the four missing category services, this
    /// block is not reached by a per-employee routing decision: it runs for EVERY employee, so throwing would
    /// fail 100% of runs and make the rest of the orchestration untestable.</para>
    ///
    /// <para><b>Also note</b> the wage-code annotations in <see cref="IPayrollDayCountService"/>'s XML docs do
    /// not all match legacy's actual <c>CalculateTimeType</c> call sites (e.g. it labels
    /// <c>GetSickLeavePaidDaysAsync</c> as 4003, but legacy line 2051 writes Sick Leave Paid Days to
    /// <b>4002</b> and line 2071 writes Sick Leave UNPAID to <b>4003</b>). Whoever ports this region must
    /// take the codes from the legacy call sites, not from those doc comments.</para>
    /// </summary>
    private Task RunTimeTypeCumulationAsync(PayrollCalculationContext context)
    {
        return Task.CompletedTask;
    }

    #endregion
}
