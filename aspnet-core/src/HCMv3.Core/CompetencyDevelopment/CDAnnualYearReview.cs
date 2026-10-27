using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// One weighted <see cref="Competency"/> annual-year review entry within a <see cref="CDForm"/> (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.CompetencyDevelopment.CDAnnualYearReview</c>, an aggregated
/// child of <see cref="CDForm"/> -- see its class remarks). <see cref="CDRatingId"/> is optional, mirroring
/// the legacy's non-required <c>Rating</c> reference property.
/// </summary>
public class CDAnnualYearReview : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int CDFormId { get; set; }

    public virtual CDForm CDForm { get; set; }

    public virtual int CompetencyId { get; set; }

    public virtual Competency Competency { get; set; }

    public virtual double Weight { get; set; }

    [StringLength(512)]
    public virtual string Target { get; set; }

    [StringLength(500)]
    public virtual string EmployeeComments { get; set; }

    [StringLength(500)]
    public virtual string ManagerComments { get; set; }

    public virtual int? CDRatingId { get; set; }

    public virtual CDRating CDRating { get; set; }
}
