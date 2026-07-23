using System;
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
/// DEFERRED / DROPPED (not ported):
/// - OnHold record auto-creation/removal (<c>OnHoldFromDate</c>/<c>OnHoldTillDate</c>/<c>OnHoldAll</c>/
///   <c>OnHoldBasicPay</c>/etc. and the <c>SaveOtherInfo</c> OnHold block) -- <c>EmployeeOnHold</c> not ported.
/// - TimeRegister deletion after LastDayInService -- TimeManagement not ported this wave.
/// - <c>SetRetroDate</c> payroll-retro logic -- Payroll not ported.
/// - Indemnity pre-accrual ("advance EoS") -- Gratuity/Indemnity not ported.
/// - <c>ValidatePreviousEntry</c> chain-blocking against FnF/Rejoin/OnHold/SalaryRegister state -- none
///   of those entities exist in this wave.
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
}
