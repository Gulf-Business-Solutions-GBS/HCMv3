using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// One answered line of an <see cref="EmployeeClearance"/> checklist (ported from legacy XAF
/// <c>ClearanceRequestDetail</c>).
///
/// <para>
/// A row exists only once somebody has answered the line, and it records who: <see cref="ApproverId"/> is
/// the employee who filled it in, which is the audit trail for "who said the laptop came back". The answer
/// is free text rather than a flag because legacy's checklist lines are not all yes/no -- a settlement
/// amount or an asset serial is as likely as "returned".
/// </para>
/// </summary>
public class EmployeeClearanceDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeClearanceId { get; set; }

    public virtual EmployeeClearance EmployeeClearance { get; set; }

    /// <summary>The checklist line being answered.</summary>
    public virtual int ClearanceRequestFieldId { get; set; }

    public virtual ClearanceRequestField ClearanceRequestField { get; set; }

    [Required]
    [StringLength(1000)]
    public virtual string Value { get; set; }

    /// <summary>The employee who answered this line, i.e. the department's approver.</summary>
    public virtual int ApproverId { get; set; }

    public virtual Employee Approver { get; set; }
}
