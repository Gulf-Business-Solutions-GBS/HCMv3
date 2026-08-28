using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ReportManagement;

/// <summary>
/// A RULE for deciding who receives a burst slice -- not an address.
///
/// WHY A RULE AND NOT AN ADDRESS. A schedule outlives the org chart. If the Finance department's
/// secretary changes, or a manager takes over another team, a stored address keeps delivering payroll
/// and attendance data to the wrong person indefinitely, silently. Rules are re-evaluated on every run,
/// so the distribution list follows the org chart automatically.
///
/// <see cref="ReportRecipientRule.RoleScoped"/> is the workhorse. It resolves through
/// <c>SysUserOrgUnitData</c>, which maps Employee x Role x OrgLevel -- so "the Department Secretary"
/// means the secretary OF THE SLICE'S DEPARTMENT when <see cref="ScopeToBurstUnit"/> is set, and every
/// secretary everywhere when it is not. The customer's HR Team, Payroll Team, Finance Team, Time Admins
/// and Department Secretary are all this one rule with different <see cref="RoleId"/> values.
/// </summary>
public class ReportScheduleRecipient : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ReportScheduleId { get; set; }

    public virtual ReportSchedule ReportSchedule { get; set; }

    public virtual ReportRecipientRule Rule { get; set; }

    /// <summary>Required for <see cref="ReportRecipientRule.RoleScoped"/>; ignored otherwise.</summary>
    public virtual int? RoleId { get; set; }

    /// <summary>
    /// <see cref="ReportRecipientRule.RoleScoped"/> only. When true the role is matched within the
    /// slice's own organization unit, giving "this department's secretary". When false the role is
    /// matched tenant-wide, giving "the whole HR team" -- appropriate for a central team that should
    /// receive every slice.
    /// </summary>
    public virtual bool ScopeToBurstUnit { get; set; }

    /// <summary>Required for <see cref="ReportRecipientRule.Explicit"/>; ignored otherwise.</summary>
    [StringLength(256)]
    [EmailAddress]
    public virtual string EmailAddress { get; set; }

    /// <summary>
    /// Sends the slice without applying this recipient's row-level-security scope.
    ///
    /// DEFAULTS TO FALSE AND SHOULD STAY THAT WAY. It exists only for
    /// <see cref="ReportRecipientRule.Explicit"/> recipients who are not HCMv3 users at all -- an
    /// external auditor has no RLS scope, so scoping them would produce an empty file every time.
    /// Setting it on an internal recipient deliberately hands them data they cannot otherwise see, so
    /// the app service refuses the combination unless the caller holds the elevated permission.
    /// </summary>
    public virtual bool BypassRecipientScope { get; set; }
}
