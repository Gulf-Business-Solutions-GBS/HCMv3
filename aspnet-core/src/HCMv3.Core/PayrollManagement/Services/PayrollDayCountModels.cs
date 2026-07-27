using System;
using System.Collections.Generic;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// One month-wise "days" row produced by <see cref="IPayrollDayCountService.GetPresentDaysAsync"/> during an
/// off-cycle (advance-leave recovery) run.
///
/// <para>Ported from the legacy PayrollEngine's private <c>Program.SalaryRegisterDays</c> DTO class
/// (<c>HCM10.PayrollEngine/Program.cs</c> line 5538), which legacy accumulated through a
/// <c>ref List&lt;SalaryRegisterDays&gt;</c> out-parameter threaded through most of the day/amount
/// calculators and eventually persisted as <see cref="SalaryRegisterDaysDetail"/> rows. This port keeps the
/// same four fields but returns them (see <see cref="PresentDaysResult"/>) instead of mutating a caller-owned
/// list, since <c>ref</c> parameters are not usable on <c>async</c> signatures.</para>
///
/// <para>This is deliberately NOT the <see cref="SalaryRegisterDaysDetail"/> entity: that entity requires a
/// persisted <c>SalaryRegisterDetailId</c>, which does not exist yet at the point this calculation runs
/// (legacy only materialised the entity rows later, in <c>CalculateTimeType</c>). Whoever ports
/// <c>CalculateTimeType</c> is responsible for turning these into <see cref="SalaryRegisterDaysDetail"/>
/// rows.</para>
/// </summary>
public class SalaryRegisterDayCount
{
    /// <summary>The month this row belongs to, represented as that month's LAST day (legacy convention).</summary>
    public DateTime Period { get; set; }

    /// <summary>Always 0 as produced by <c>GetPresentDays</c> (legacy sets it later, in <c>CalculateTimeType</c>).</summary>
    public double Rate { get; set; }

    /// <summary>The present/working days computed for <see cref="Period"/>.</summary>
    public double Number { get; set; }

    /// <summary>Always 0 as produced by <c>GetPresentDays</c> (legacy sets it later, in <c>CalculateTimeType</c>).</summary>
    public double Amount { get; set; }
}

/// <summary>
/// Return shape of <see cref="IPayrollDayCountService.GetPresentDaysAsync"/>, replacing the legacy method's
/// <c>double</c> return value plus <c>ref List&lt;SalaryRegisterDays&gt; salaryRegisterDaysList</c>
/// out-parameter (legacy call site: <c>HCM10.PayrollEngine/Program.cs</c> line 2201).
///
/// <para>NOTE the semantic change forced by dropping <c>ref</c>: legacy APPENDED to a list shared across many
/// calculators for the same salary register. <see cref="MonthWiseDays"/> here contains ONLY the rows this
/// call produced; the caller is responsible for appending them to whatever aggregate list it maintains.</para>
/// </summary>
public class PresentDaysResult
{
    /// <summary>Total present (paid working) days across all recovery periods. Never negative.</summary>
    public double PresentDays { get; set; }

    /// <summary>
    /// Month-wise breakdown, populated ONLY when legacy would have populated it, i.e. when the run is
    /// <see cref="PayrollRunType.OffCycle"/> AND <c>SalaryRegister.InPeriod == SalaryRegister.ForPeriod</c>.
    /// Otherwise empty (never null).
    ///
    /// <para>(Legacy also gated this on <c>Program.calculateSalRegDaysDetail</c>; that field is a hardcoded
    /// <c>true</c> literal in the legacy source, never assigned from settings, args or anywhere else --
    /// verified by full-file search -- so the gate is a constant and is not ported as a parameter.)</para>
    /// </summary>
    public List<SalaryRegisterDayCount> MonthWiseDays { get; set; } = new List<SalaryRegisterDayCount>();
}

/// <summary>
/// The two off-cycle reason strings the day-count algorithms branch on, ported from the legacy
/// <c>Program.endOfServiceOffcycle</c> / <c>Program.reasonAdvanceLeave</c> string literals
/// (<c>HCM10.PayrollEngine/Program.cs</c> lines 148-149).
///
/// <para>Legacy compared these against the free-text <c>PayrollReasonType</c> name flowing through the run.
/// Deliberately named <c>PayrollDayCountOffCycleReasons</c> rather than a generic
/// <c>PayrollOffCycleReasons</c> to avoid colliding with the parallel <c>IPayrollEngineService</c> wave, which
/// may introduce its own copy; if both land, collapse them into one shared class in a follow-up.</para>
/// </summary>
public static class PayrollDayCountOffCycleReasons
{
    /// <summary>Legacy <c>Program.endOfServiceOffcycle</c>.</summary>
    public const string EndOfService = "End Of Service";

    /// <summary>Legacy <c>Program.reasonAdvanceLeave</c>.</summary>
    public const string AdvanceLeave = "Advance Leave";
}
