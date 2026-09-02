using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// One calculated day inside a <see cref="TimeLogApproval"/>, together with what the requester is PROPOSING
/// that day should read.
///
/// <para>
/// <b>This separation is the fix.</b> Legacy had no detail table: the proposed figures were pushed onto
/// <see cref="TimeRegister"/>'s <c>Approved*</c> columns the moment the request was submitted, so a timesheet
/// correction was live before it was approved and survived a rejection. Holding the proposal here means the
/// <see cref="TimeRegister"/> row keeps saying what the engine calculated until an approver says otherwise.
/// </para>
/// </summary>
public class TimeLogApprovalDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int TimeLogApprovalId { get; set; }

    public virtual TimeLogApproval TimeLogApproval { get; set; }

    public virtual int TimeRegisterId { get; set; }

    public virtual TimeRegister TimeRegister { get; set; }

    /// <summary>
    /// The working-day figure the requester was SHOWN for this day, kept for audit. Deliberately NOT trusted
    /// on approval: the handler recomputes <see cref="TimeRegister.ApprovedActualWorkingDay"/> from the day's
    /// own minutes (legacy <c>TimeLogRepository.cs</c> lines 311-336), because the engine may have
    /// recalculated the row between submission and approval.
    /// </summary>
    public virtual double ProposedActualWorkingDay { get; set; }

    /// <summary>
    /// Proposed <see cref="TimeRegister.ApprovedAbsentDay"/>: 1 = the day stays an absence, 0 = it is being
    /// forgiven. Legacy's grid rendered this as a checkbox.
    /// </summary>
    public virtual double ProposedAbsentDay { get; set; }

    /// <summary>
    /// Proposed <see cref="TimeRegister.ApprovedDedLateInEarlyOutMins"/> -- the deductible late-in /
    /// early-out minutes the approver is being asked to accept, usually lower than the calculated figure.
    /// </summary>
    public virtual double ProposedDedLateInEarlyOutMins { get; set; }
}
