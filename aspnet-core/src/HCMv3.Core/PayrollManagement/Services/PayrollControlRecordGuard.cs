using System;
using Abp.UI;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The <see cref="PayrollControlRecord"/> gates the legacy engine applies BEFORE it will create a single
/// <see cref="SalaryRegister"/> row, extracted verbatim from
/// <c>HCM10.PayrollEngine/Program.Main</c> into one pure, side-effect-free, fully unit-testable place.
///
/// <para><b>Why this is a static helper and not a domain service.</b> Every one of these rules is a pure
/// function of (control record, run type, dates) -- legacy performed them inline on an already-loaded
/// <c>payrollControlRecord</c> object. Keeping them repository-free means the wave that ports
/// <c>IPayrollEngineService</c> loads the control record however it likes and simply calls these methods;
/// there is no DI wiring to get wrong and no query to duplicate.</para>
///
/// <para><b>These are financial-integrity controls, not validation niceties.</b> Skipping
/// <see cref="EnsureRegularRunAllowed"/> lets a payroll run write results into a period the business has
/// already locked; skipping <see cref="EnsureEmployeeControlRecordMatches"/> lets an off-cycle run silently
/// skip a whole month of payroll (that is exactly what legacy's "add check to Not skip any month for payroll"
/// comment at <c>Program.cs</c> line 844 is guarding against). Wave P2's engine MUST call all three. Their
/// unit tests live in <c>test/HCMv3.Tests/PayrollManagement/PayrollControlRecordGuard_Tests.cs</c>.</para>
///
/// <para><b>ADAPTATION.</b> Legacy's <c>PeriodLocked</c>/<c>PeriodClosed</c> enums (No=0/Yes=1) were
/// simplified to <see cref="bool"/> when <see cref="PayrollControlRecord"/> was ported (see that entity's
/// remarks), so legacy's <c>== PeriodLocked.Yes</c> becomes a plain truth test. Legacy's Arabic/English
/// message pair (selected by the <c>args[7]</c> UI culture code, which this port does not carry) collapses to
/// the English message only -- localisation is the caller's concern in ABP.</para>
/// </summary>
public static class PayrollControlRecordGuard
{
    /// <summary>
    /// Legacy's ubiquitous <c>new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month), 0, 0, 0)</c>
    /// idiom: the last day of <paramref name="date"/>'s month, at midnight.
    /// </summary>
    public static DateTime MonthEnd(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 0, 0, 0);
    }

    /// <summary>
    /// The pay-in date of the period IMMEDIATELY AFTER the one the control record currently sits on:
    /// <c>MonthEnd(CalendarMonthEnd + 1 day)</c>. Legacy computes this in two places -- when advancing
    /// <c>payInDate</c> on a time-engine-autorun off-cycle run (<c>Program.cs</c> lines 295-296) and when
    /// asserting the run is not skipping a month (lines 844-848) -- and the two MUST agree, which is why they
    /// share one expression here.
    /// </summary>
    public static DateTime NextPeriodPayInDate(PayrollControlRecord record)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        return MonthEnd(record.CalendarMonthEnd.AddDays(1));
    }

    /// <summary>
    /// Legacy <c>Program.cs</c> lines 266-276 (the Regular arm): a regular run is allowed ONLY when the
    /// control record's <c>CalendarMonthEnd</c> falls on the run's <paramref name="payInDate"/> AND the period
    /// is LOCKED. Anything else throws "Payroll Control Record not locked or is not current."
    ///
    /// <para>Note the comparison is on <c>.Date</c> in legacy -- a control record stamped with a time
    /// component must still match. Preserved.</para>
    /// </summary>
    public static void EnsureRegularRunAllowed(PayrollControlRecord record, DateTime payInDate)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (record.CalendarMonthEnd.Date == payInDate.Date && record.PeriodLocked)
        {
            return;
        }

        throw new UserFriendlyException("Payroll Control Record not locked or is not current.");
    }

    /// <summary>
    /// Legacy <c>Program.cs</c> lines 280-347 (the OffCycle arm), returning the pay-in date the run must
    /// actually use. Both legacy sub-branches are reproduced exactly:
    ///
    /// <list type="bullet">
    /// <item><description><b>Employee code is mandatory</b> on ANY off-cycle run (legacy lines 285-291 and
    /// 314-320): blank throws "No Employee Code found, Cannot Run OffCycle Payroll Run."</description></item>
    /// <item><description><b><paramref name="enableTimeEngineAutorun"/> == true</b> (legacy lines 294-307):
    /// requires <c>PeriodClosed</c>, and ADVANCES the pay-in date to
    /// <see cref="NextPeriodPayInDate"/> -- i.e. the off-cycle is booked into the period AFTER the closed one.
    /// <paramref name="offCyclePaymentDate"/> is deliberately NOT consulted on this arm, matching
    /// legacy.</description></item>
    /// <item><description><b><paramref name="enableTimeEngineAutorun"/> == false</b> (legacy lines 311-346):
    /// requires a non-blank <paramref name="offCyclePaymentDate"/> ("offCycle PaymentDate not entered."),
    /// requires <c>CalendarMonthEnd &lt; offCyclePaymentDate</c> AND <c>PeriodClosed</c>, and sets the pay-in
    /// date to the MONTH END OF THE PAYMENT DATE (legacy line 335, with its own worked example: "if
    /// offcyclepayment date in 05.12.2017 =&gt; payindate will be 31.12.2017").</description></item>
    /// </list>
    ///
    /// <para>Both failure arms throw legacy's "Payroll Control Record not Closed or is not current." -- note
    /// this differs from the Regular arm's "not locked" wording, and the difference is legacy's.</para>
    /// </summary>
    /// <param name="record">The payroll area's control record.</param>
    /// <param name="payInDate">The requested pay-in date (legacy <c>args[2]</c>), returned unchanged only if no branch adjusts it.</param>
    /// <param name="offCyclePaymentDate">Legacy <c>args[5]</c>.</param>
    /// <param name="employeeCodes">Legacy <c>args[1]</c>: the comma-separated employee code list.</param>
    /// <param name="enableTimeEngineAutorun">Legacy <c>Program.enableTimeEngineAutorun</c>.</param>
    public static DateTime ResolveOffCyclePayInDate(
        PayrollControlRecord record,
        DateTime payInDate,
        DateTime offCyclePaymentDate,
        string employeeCodes,
        bool enableTimeEngineAutorun)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (string.IsNullOrWhiteSpace(employeeCodes))
        {
            throw new UserFriendlyException("No Employee Code found, Cannot Run OffCycle Payroll Run.");
        }

        if (enableTimeEngineAutorun)
        {
            if (!record.PeriodClosed)
            {
                throw new UserFriendlyException("Payroll Control Record not Closed or is not current.");
            }

            return NextPeriodPayInDate(record);
        }

        if (offCyclePaymentDate.Date == default(DateTime))
        {
            throw new UserFriendlyException("offCycle PaymentDate not entered.");
        }

        if (record.CalendarMonthEnd.Date < offCyclePaymentDate.Date && record.PeriodClosed)
        {
            return MonthEnd(offCyclePaymentDate);
        }

        throw new UserFriendlyException("Payroll Control Record not Closed or is not current.");
    }

    /// <summary>
    /// Legacy <c>Program.cs</c> lines 840-849 -- the PER-EMPLOYEE re-check, run after the employee's own
    /// payroll area (and therefore its own control record) has been resolved. Two distinct throws:
    ///
    /// <list type="number">
    /// <item><description><c>payInDate &lt; CalendarMonthEnd</c> =&gt; "Payroll Control Record set to run for
    /// period later than payroll run date." -- the control record is AHEAD of the run, i.e. the period has
    /// already moved on.</description></item>
    /// <item><description>On an <b>off-cycle</b> run only, <c>NextPeriodPayInDate != payInDate</c> =&gt;
    /// "Mismatch between Payroll Control Record and Payroll run date. PayrollArea : {name}". Legacy's own
    /// comment on this line is "add check to Not skip any month for payroll" -- without it an off-cycle run
    /// dated two months out would quietly leave the intervening month unpaid.</description></item>
    /// </list>
    ///
    /// <para>Note the first check compares against the raw <c>CalendarMonthEnd</c> (NOT <c>.Date</c>), the
    /// second against the month-end normalisation. That asymmetry is legacy's and is preserved.</para>
    /// </summary>
    public static void EnsureEmployeeControlRecordMatches(
        PayrollControlRecord record,
        PayrollArea payrollArea,
        PayrollRunType payrollRunType,
        DateTime payInDate)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (payInDate < record.CalendarMonthEnd)
        {
            throw new UserFriendlyException("Payroll Control Record set to run for period later than payroll run date.");
        }

        if (payrollRunType == PayrollRunType.OffCycle && NextPeriodPayInDate(record) != payInDate)
        {
            throw new UserFriendlyException(string.Format(
                "Mismatch between Payroll Control Record and Payroll run date. PayrollArea : {0}",
                payrollArea?.Name));
        }
    }
}
