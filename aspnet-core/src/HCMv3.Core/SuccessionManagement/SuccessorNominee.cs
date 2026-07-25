using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// A nominated successor for an employee's position (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.SuccessorNominee</c>). Legacy validation (required
/// fields, no duplicate Employee+Nominee pair) is reproduced in <c>SuccessorNomineesAppService</c>.
/// </summary>
public class SuccessorNominee : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int NomineeId { get; set; }

    public virtual Employee Nominee { get; set; }

    public virtual int ReadinessId { get; set; }

    public virtual Readiness Readiness { get; set; }

    public virtual int RankingId { get; set; }

    public virtual Ranking Ranking { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
