using System;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The compensation figures on one Full &amp; Final (F&amp;F) entry -- the part of legacy
/// <c>EmployeeFnFPayment</c> that drives the quota side effects.
///
/// <para><b>Two of these travel together on every save.</b> Legacy captured the values it had LOADED
/// (<c>OnLoaded</c>, the <c>prev*</c> fields) and compared them against the values being SAVED, because an edit
/// must first undo what the previous save wrote -- otherwise changing the compensation date or the days would
/// leave the old gratuity row in place next to the new one, and the end-of-service off-cycle would pay both.
/// <see cref="IFullAndFinalSettlementService"/> takes the saved values as <c>current</c> and the loaded ones as
/// <c>previous</c>; <c>previous</c> is <c>null</c> on a new entry (legacy's <c>Oid == -1</c>).</para>
/// </summary>
public class FullAndFinalValues
{
    public int EmployeeId { get; set; }

    public DateTime CompensationDate { get; set; }

    /// <summary>Annual leave paid out -- written as an Encashment <c>EmployeeAbsAdjustment</c>.</summary>
    public double LeaveCompensateDays { get; set; }

    /// <summary>
    /// Indemnity days paid out -- written as an <c>EmployeeGratuity</c> row, which is what the end-of-service
    /// off-cycle's wage code 4005 reads. <b>This is the bridge from F&amp;F to payroll.</b>
    /// </summary>
    public double GratuityCompensateDays { get; set; }

    /// <summary>A manual indemnity amount, written as an "Indemnity Pay Adjust" <c>EmployeePayment</c>.</summary>
    public double IndemnityPayAdjustment { get; set; }

    public double TicketCompensateCount { get; set; }

    public double Dependant1TicketCompensateCount { get; set; }

    public double Dependant2TicketCompensateCount { get; set; }

    public double Dependant3TicketCompensateCount { get; set; }
}

/// <summary>
/// What an F&amp;F entry offers the operator: the balances available to compensate, and the SUGGESTED indemnity
/// days. Legacy computed these as non-persisted properties on the business object; here they are served
/// separately so the screen can show them before anything is saved.
/// </summary>
public class FullAndFinalBalances
{
    /// <summary>Legacy <c>LeaveAccrualDays</c>: annual-leave quota accrued.</summary>
    public double LeaveAccrualDays { get; set; }

    /// <summary>Legacy <c>LeaveRemainingDays</c>: annual-leave quota balance -- the most leave that can be paid.</summary>
    public double LeaveRemainingDays { get; set; }

    /// <summary>
    /// Legacy <c>GratuityAccrualDays</c> ("Indemnity Accrual Days"): the FULL indemnity accrued, capped. This is
    /// the PROVISION figure and is the SAME for resignation and termination.
    /// </summary>
    public double GratuityAccrualDays { get; set; }

    /// <summary>Legacy <c>GratuityRemDays</c> ("Indemnity Remaining Days"): the indemnity quota balance.</summary>
    public double GratuityRemainingDays { get; set; }

    /// <summary>
    /// Legacy <c>SuggestedCompensateDays</c>: the indemnity days to PAY. Accrued days times the
    /// <c>IndemnityPayRule</c> percentage for the employee's separation type and service band -- so it is where
    /// a resignation is paid less than a termination. <b>Zero when no rule matches</b>, by legacy's own design.
    /// </summary>
    public double SuggestedIndemnityDays { get; set; }

    public double TicketAccrualCount { get; set; }

    public double TicketRemainingCount { get; set; }

    public double Dependant1TicketCount { get; set; }

    public double Dependant1TicketRemainingCount { get; set; }

    public double Dependant2TicketCount { get; set; }

    public double Dependant2TicketRemainingCount { get; set; }

    public double Dependant3TicketCount { get; set; }

    public double Dependant3TicketRemainingCount { get; set; }

    /// <summary>
    /// Legacy <c>LeaveCompensateDays</c> auto-fill on a new entry: the whole leave balance, when
    /// <c>AutoFillFnFCompensation</c> is on. Zero otherwise.
    /// </summary>
    public double AutoFillLeaveCompensateDays { get; set; }

    /// <summary>
    /// Legacy <c>GratuityCompensateDays</c> auto-fill on a new entry: the SMALLER of the suggested days and the
    /// remaining balance, when <c>AutoFillFnFCompensation</c> is on. Zero otherwise. Tickets are never
    /// auto-filled -- that is legacy's, not an omission.
    /// </summary>
    public double AutoFillGratuityCompensateDays { get; set; }
}
