using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Peer nominated by an employee for peer-review, for a given <see cref="PAPeriod"/> (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAPeerNominee</c>).
/// </summary>
public class PAPeerNominee : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAPeriodId { get; set; }

    public virtual PAPeriod PAPeriod { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>The nominated peer reviewer.</summary>
    public virtual int PeerId { get; set; }

    public virtual Employee Peer { get; set; }

    public virtual bool Submitted { get; set; }
}
