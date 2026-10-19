using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// One employee's day inside an <see cref="OvertimeBatch"/> (legacy <c>OvertimeRequestDetail</c>).
///
/// <para>
/// The time fields are a SNAPSHOT of the employee's <c>TimeRegister</c> day at the moment the batch was saved
/// -- legacy's shift-report columns (Shift Start/End, Sign In/Out, Planned Hours, Total Hours, Late In, Early
/// Out). They are copied server-side from the time register, never taken from the browser, because
/// <see cref="TotalHours"/> becomes the overtime row's actual hours on approval.
/// </para>
///
/// <para>
/// Deliberately NOT workflow-enabled: what is approved is the batch, whole.
/// </para>
/// </summary>
public class OvertimeBatchDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int OvertimeBatchId { get; set; }

    public virtual OvertimeBatch OvertimeBatch { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>Legacy <c>Date</c>: the day the overtime was worked. Becomes <see cref="EmployeeOvertime.EffectiveDate"/>.</summary>
    public virtual DateTime Date { get; set; }

    /// <summary>The time-register day this line was loaded from. A bare id, no navigation.</summary>
    public virtual int? TimeRegisterId { get; set; }

    /// <summary>Legacy <c>ShiftStart</c> (was a display string there; the time itself here).</summary>
    public virtual DateTime? ShiftStart { get; set; }

    public virtual DateTime? ShiftEnd { get; set; }

    /// <summary>Legacy <c>SignIn</c>: the actual first punch of the day.</summary>
    public virtual DateTime? SignIn { get; set; }

    public virtual DateTime? SignOut { get; set; }

    /// <summary>Legacy <c>PlannedHours</c>, in hours.</summary>
    public virtual double PlannedHours { get; set; }

    /// <summary>
    /// Legacy <c>TotalHours</c>: the overtime the day actually produced (legacy shift mode stored the report's
    /// "Overtime Hours" column here). On approval it becomes both <see cref="EmployeeOvertime.OvertimeHours"/>
    /// and <see cref="EmployeeOvertime.ActualOvertimeHours"/>, rounded to two places as legacy did.
    /// </summary>
    public virtual double TotalHours { get; set; }

    /// <summary>Legacy <c>ApprovedHours</c>: what the requester proposes to pay. Becomes ApprovedOvertimeHours.</summary>
    public virtual double ApprovedHours { get; set; }

    /// <summary>Legacy <c>LateIn</c>, in minutes, as the time register holds it.</summary>
    public virtual double LateIn { get; set; }

    /// <summary>Legacy <c>EarlyOut</c>, in minutes.</summary>
    public virtual double EarlyOut { get; set; }

    /// <summary>
    /// Legacy <c>OvertimeType</c> on the line (added 2025-09). Shown on the screen. NOTE: legacy approval did NOT
    /// use it -- every overtime row it wrote got the type with code "4" -- and neither does this port; see
    /// <c>Services.OvertimeBatchService.LegacyApprovalOvertimeTypeCode</c>.
    /// </summary>
    public virtual int? OvertimeTypeId { get; set; }

    public virtual OvertimeType OvertimeType { get; set; }

    /// <summary>Legacy <c>Comments</c>, Size(1000).</summary>
    [StringLength(1000)]
    public virtual string Comments { get; set; }

    /// <summary>
    /// The overtime row approval created from this line. Null until then. This is what makes approval safe to
    /// run twice: a line that already has one is skipped.
    /// </summary>
    public virtual int? EmployeeOvertimeId { get; set; }
}
