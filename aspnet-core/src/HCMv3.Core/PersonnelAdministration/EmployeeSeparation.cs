using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee separation (exit) record -- DELIBERATELY MINIMAL "record-keeping only" port of legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeSeparation</c> (Wave B). This is the
/// biggest simplification in Wave B: the legacy entity is deeply coupled to Payroll/Loan/Gratuity/
/// Ticket/TimeManagement modules that are not ported.
///
/// PORTED SINCE: the on-hold block. The <c>OnHold*</c> fields below are legacy's, and saving them creates,
/// replaces or removes the matching <see cref="EmployeeOnHold"/> row exactly as legacy <c>SaveOtherInfo</c>
/// and <c>OnDeleting</c> did -- see <see cref="Services.ISeparationOnHoldService"/>. Payroll reads that row,
/// never these flags. Legacy's two "an on-hold was entered after this one" refusals are ported with it.
///
/// DEFERRED / DROPPED (not ported):
/// - TimeRegister deletion after LastDayInService -- TimeManagement not ported this wave.
/// - <c>SetRetroDate</c> payroll-retro logic -- Payroll not ported.
/// - Indemnity pre-accrual ("advance EoS") -- Gratuity/Indemnity not ported.
/// - <c>ValidatePreviousEntry</c> chain-blocking against FnF/Rejoin/SalaryRegister state (the OnHold part of
///   it IS ported, see above).
/// - Any interdependent auto-recompute between SubmissionDate/LastDayInService/NoticePeriodDays/
///   PlannedLastDayInService beyond simple validation (legacy <c>RecomputeLastDayInService</c>/
///   <c>RecomputePlannedLastDate</c>/<c>RecomputeNoticePeriod</c> UI-binding side effects).
/// - <c>DocAttachment</c> auto-creation (attachment/document module not in scope this wave).
///
/// Only required-field and <c>LastDayInService &gt;= SubmissionDate</c> validation is ported -- see
/// <c>EmployeeSeparationsAppService</c>.
/// </summary>
public class EmployeeSeparation : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime SubmissionDate { get; set; }

    public virtual DateTime LastDayInService { get; set; }

    public virtual int NoticePeriodDays { get; set; }

    public virtual DateTime PlannedLastDayInService { get; set; }

    public virtual SeparationType SeparationType { get; set; }

    public virtual int SeparationReasonId { get; set; }

    public virtual SeparationReason SeparationReason { get; set; }

    /// <summary>
    /// Set when this separation record is revoked; <c>null</c> means still active/effective.
    /// </summary>
    public virtual DateTime? RevokeDate { get; set; }

    /// <summary>
    /// Optional link to the <see cref="EmployeeAction"/> that produced this record.
    /// </summary>
    public virtual int? EmployeeActionId { get; set; }

    public virtual EmployeeAction EmployeeAction { get; set; }

    // ---- On-hold information (legacy region "OnHold Information") ----

    /// <summary>
    /// Start of the pay hold. Normalised to the first of the month on save when any hold flag is ticked, as in
    /// legacy, and that normalised date is what identifies the <see cref="EmployeeOnHold"/> row it created.
    /// </summary>
    public virtual DateTime? OnHoldFromDate { get; set; }

    /// <summary>End of the pay hold. Normalised to the last day of its month on save when any flag is ticked.</summary>
    public virtual DateTime? OnHoldTillDate { get; set; }

    /// <summary>Legacy <c>OnHoldAll</c>; copied to <see cref="EmployeeOnHold.OnHold"/>.</summary>
    public virtual bool OnHoldAll { get; set; }

    public virtual bool OnHoldBasicPay { get; set; }

    public virtual bool OnHoldLeavePay { get; set; }

    public virtual bool OnHoldIndemnityPay { get; set; }

    public virtual bool OnHoldAirTicketPay { get; set; }

    public virtual bool OnHoldLoanDeduction { get; set; }

    public virtual bool OnHoldAllEarnings { get; set; }

    public virtual bool OnHoldAllDeductions { get; set; }

    /// <summary>Legacy <c>BankTransfer</c> (2025.06.11): hold the bank transfer entry as well.</summary>
    public virtual OnHoldStatus BankTransfer { get; set; }

    // ---- Revoke information / comments ----

    /// <summary>Legacy <c>RevokeComments</c>, Size(500).</summary>
    [StringLength(500)]
    public virtual string RevokeComments { get; set; }

    /// <summary>Legacy <c>Comments</c>, Size(500).</summary>
    [StringLength(500)]
    public virtual string Comments { get; set; }
}
